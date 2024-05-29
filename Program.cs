using AmazingFileVersionControl.Core.Contexts;
using AmazingFileVersionControl.Core.Repositories;
using AmazingFileVersionControl.Core.Infrastructure;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace AmazingFileVersionControl.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            });

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
            });

            // Configure services
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure middleware
            Configure(app);

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add DbContext for PostgreSQL
            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgreSqlConnection")));

            // Add MongoDB client
            services.AddSingleton<IMongoClient, MongoClient>(sp =>
                new MongoClient(configuration.GetConnectionString("MongoDbConnection")));

            // Add repository services
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFileRepository, FileRepository>(sp =>
                new FileRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    configuration["MongoDbSettings:FileStorage"]));

            // Add Logging repository
            services.AddScoped<ILoggingRepository, LoggingRepository>(sp =>
                new LoggingRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    configuration["MongoDbSettings:LogStorage"]));

            // Add infrastructure services
            services.AddSingleton<IJwtGenerator, JwtGenerator>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // Add service implementations
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILoggingService, LoggingService>(); // Add LoggingService

            // Configure JWT authentication
            var key = Encoding.ASCII.GetBytes(configuration["JwtConfig:Secret"]);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = configuration["JwtConfig:Issuer"],
                    ValidAudience = configuration["JwtConfig:Audience"],
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireRole("USER", "ADMIN"));

                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("ADMIN"));
            });

            services.AddControllers();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AmazingFileVersionControl API", Version = "v1" });

                // Add JWT token support to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }

        private static void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AmazingFileVersionControl API V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
            });

            app.MapControllers();
        }
    }
}
