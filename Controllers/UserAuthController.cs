using AmazingFileVersionControl.Core.DTOs.AuthDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace AmazingFileVersionControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILoggingService _loggingService;

        public UserAuthController(IAuthService authService, ILoggingService loggingService)
        {
            _authService = authService;
            _loggingService = loggingService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            try
            {
                var token = await _authService.RegisterAsync(request.Login, request.Email, request.Password);
                await _loggingService.LogAsync(nameof(UserAuthController), nameof(Register), "User registered successfully", additionalData: new BsonDocument { { "Login", request.Login }, { "Email", request.Email } });
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserAuthController), nameof(Register), ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            try
            {
                var token = await _authService.LoginAsync(request.LoginOrEmail, request.Password);
                await _loggingService.LogAsync(nameof(UserAuthController), nameof(Login), "User logged in successfully", additionalData: new BsonDocument { { "LoginOrEmail", request.LoginOrEmail } });
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserAuthController), nameof(Login), ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
