/**
 * @file AuthController.cs
 * @brief Контроллер для аутентификации пользователей.
 */

using AmazingFileVersionControl.Core.DTOs.AuthDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace AmazingFileVersionControl.Api.Controllers
{
    /**
     * @class AuthController
     * @brief Класс контроллера для аутентификации и регистрации пользователей.
     */
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILoggingService _loggingService;

        /**
         * @brief Конструктор класса AuthController.
         * @param authService Сервис аутентификации.
         * @param loggingService Сервис логирования.
         */
        public AuthController(IAuthService authService, ILoggingService loggingService)
        {
            _authService = authService;
            _loggingService = loggingService;
        }

        /**
         * @brief Регистрация нового пользователя.
         * @param registerDto DTO с данными для регистрации.
         * @return Результат выполнения действия.
         */
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = await _authService.RegisterAsync(registerDto.Login, registerDto.Email, registerDto.Password);

                await _loggingService.LogAsync(nameof(AuthController), nameof(Register),
                    "User registered successfully",
                    additionalData: new BsonDocument
                    {
                        { "Login", registerDto.Login },
                        { "Email", registerDto.Email }
                    });

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(AuthController), nameof(Register),
                    ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        /**
         * @brief Вход пользователя в систему.
         * @param loginDto DTO с данными для входа.
         * @return Результат выполнения действия.
         */
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = await _authService.LoginAsync(loginDto.Login, loginDto.Password);

                await _loggingService.LogAsync(nameof(AuthController), nameof(Login),
                    "User logged in successfully",
                    additionalData: new BsonDocument { { "Login", loginDto.Login } });

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(AuthController),
                    nameof(Login), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
