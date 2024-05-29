/**
 * @file Program.cs
 * @brief Основная точка входа для приложения.
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
     * @brief Класс, представляющий точку входа для приложения.
     */
    public class Program
    {
        /**
         * @brief Основная точка входа для приложения.
         * @param args Аргументы командной строки.
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

            // Конфигурация сервисов
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Конфигурация промежуточного программного обеспечения
            Configure(app);

            app.Run();
        }

        /**
         * @brief Конфигурирование сервисов.
         * @param services Коллекция сервисов.
         * @param configuration Конфигурация приложения.
         */
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Добавление контекста базы данных для PostgreSQL
            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgreSqlConnection")));

            // Добавление клиента MongoDB
            services.AddSingleton<IMongoClient, MongoClient>(sp =>
                new MongoClient(configuration.GetConnectionString("MongoDbConnection")));

            // Добавление сервисов репозитория
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFileRepository, FileRepository>(sp =>
                new FileRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    configuration["MongoDbSettings:FileStorage"]));

            // Добавление репозитория логирования
            services.AddScoped<ILoggingRepository, LoggingRepository>(sp =>
                new LoggingRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    configuration["MongoDbSettings:LogStorage"]));

            // Добавление инфраструктурных сервисов
            services.AddSingleton<IJwtGenerator, JwtGenerator>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // Добавление реализаций сервисов
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILoggingService, LoggingService>(); // Добавление LoggingService

            // Конфигурация аутентификации JWT
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

            // Регистрация генератора Swagger, определение одного или нескольких документов Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AmazingFileVersionControl API", Version = "v1" });

                // Добавление поддержки токена JWT в Swagger
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
         * @brief Конфигурирование приложения.
         * @param app Веб-приложение.
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

            // Включение промежуточного ПО для обслуживания сгенерированного Swagger в виде JSON конечной точки.
            app.UseSwagger();

            // Включение промежуточного ПО для обслуживания swagger-ui (HTML, JS, CSS и т.д.),
            // указывая конечную точку Swagger JSON.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AmazingFileVersionControl API V1");
                c.RoutePrefix = string.Empty; // Установить Swagger UI в корне приложения
            });

            app.MapControllers();
        }
    }
}
