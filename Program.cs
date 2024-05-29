/**
 * @file Program.cs
 * @brief �������� ����� ����� ��� ����������.
 */

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
    /**
     * @class Program
     * @brief �����, �������������� ����� ����� ��� ����������.
     */
    public class Program
    {
        /**
         * @brief �������� ����� ����� ��� ����������.
         * @param args ��������� ��������� ������.
         */
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

            // ������������ ��������
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // ������������ �������������� ������������ �����������
            Configure(app);

            app.Run();
        }

        /**
         * @brief ���������������� ��������.
         * @param services ��������� ��������.
         * @param configuration ������������ ����������.
         */
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // ���������� ��������� ���� ������ ��� PostgreSQL
            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgreSqlConnection")));

            // ���������� ������� MongoDB
            services.AddSingleton<IMongoClient, MongoClient>(sp =>
                new MongoClient(configuration.GetConnectionString("MongoDbConnection")));

            // ���������� �������� �����������
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFileRepository, FileRepository>(sp =>
                new FileRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    configuration["MongoDbSettings:FileStorage"]));

            // ���������� ����������� �����������
            services.AddScoped<ILoggingRepository, LoggingRepository>(sp =>
                new LoggingRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    configuration["MongoDbSettings:LogStorage"]));

            // ���������� ���������������� ��������
            services.AddSingleton<IJwtGenerator, JwtGenerator>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // ���������� ���������� ��������
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILoggingService, LoggingService>(); // ���������� LoggingService

            // ������������ �������������� JWT
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

            // ����������� ���������� Swagger, ����������� ������ ��� ���������� ���������� Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AmazingFileVersionControl API", Version = "v1" });

                // ���������� ��������� ������ JWT � Swagger
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

        /**
         * @brief ���������������� ����������.
         * @param app ���-����������.
         */
        private static void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // ��������� �������������� �� ��� ������������ ���������������� Swagger � ���� JSON �������� �����.
            app.UseSwagger();

            // ��������� �������������� �� ��� ������������ swagger-ui (HTML, JS, CSS � �.�.),
            // �������� �������� ����� Swagger JSON.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AmazingFileVersionControl API V1");
                c.RoutePrefix = string.Empty; // ���������� Swagger UI � ����� ����������
            });

            app.MapControllers();
        }
    }
}
