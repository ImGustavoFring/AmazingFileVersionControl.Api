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
using System.Text;
using System.Text.Json.Serialization;

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
                    configuration["MongoDbSettings:DatabaseName"]));

            // Add infrastructure services
            services.AddSingleton<IJwtService, JwtService>();
            services.AddSingleton<IBcCryptService, BcCryptService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFileService, FileService>();

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
            });

            services.AddControllers();
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
            app.MapControllers();
        }
    }
}
