using AmazingFileVersionControl.Core.Models.UserDbEntities;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

namespace AmazingFileVersionControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILoggingService _loggingService;

        public UserController(IUserService userService, ILoggingService loggingService)
        {
            _userService = userService;
            _loggingService = loggingService;
        }

        private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value;
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsAdmin() => User.IsInRole("ADMIN");

        private async Task<bool> UserExists(string userId)
        {
            var user = await _userService.GetById(userId);
            return user != null;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUser([FromQuery] string? userId)
        {
            try
            {
                var currentUser = GetUserId();
                var targetUserId = userId ?? currentUser;

                if (!await UserExists(targetUserId))
                {
                    return NotFound("User not found.");
                }

                if (!IsAdmin() && targetUserId != currentUser)
                {
                    return Forbid("You can only view your own profile.");
                }

                var user = await _userService.GetById(targetUserId);

                await _loggingService.LogAsync(nameof(UserController), nameof(GetUser),
                    "User profile retrieved successfully",
                    additionalData: new BsonDocument { { "UserId", targetUserId } });

                return Ok(user);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(GetUser), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("search-by-login")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetUsersByLoginSubstring([FromQuery] string loginSubstring)
        {
            try
            {
                var users = await _userService.GetAllByLoginSubstring(loginSubstring);

                await _loggingService.LogAsync(nameof(UserController), nameof(GetUsersByLoginSubstring),
                    "Users retrieved by login substring successfully",
                    additionalData: new BsonDocument { { "LoginSubstring", loginSubstring } });

                return Ok(users);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(GetUsersByLoginSubstring), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("search-by-email")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetUsersByEmailSubstring([FromQuery] string emailSubstring)
        {
            try
            {
                var users = await _userService.GetAllByEmailSubstring(emailSubstring);

                await _loggingService.LogAsync(nameof(UserController), nameof(GetUsersByEmailSubstring),
                    "Users retrieved by email substring successfully",
                    additionalData: new BsonDocument { { "EmailSubstring", emailSubstring } });

                return Ok(users);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(GetUsersByEmailSubstring), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("change-login")]
        public async Task<IActionResult> ChangeLogin([FromQuery] string? userId, [FromBody] string newLogin)
        {
            try
            {
                var currentUser = GetUserId();
                var targetUserId = userId ?? currentUser;

                if (!await UserExists(targetUserId))
                {
                    return NotFound("User not found.");
                }

                if (!IsAdmin() && targetUserId != currentUser)
                {
                    return Forbid("You can only change your own login.");
                }

                await _userService.ChangeLogin(targetUserId, newLogin);

                await _loggingService.LogAsync(nameof(UserController), nameof(ChangeLogin),
                    "User login changed successfully",
                    additionalData: new BsonDocument { { "UserId", targetUserId }, { "NewLogin", newLogin } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(ChangeLogin), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("change-email")]
        public async Task<IActionResult> ChangeEmail([FromQuery] string? userId, [FromBody] string newEmail)
        {
            try
            {
                var currentUser = GetUserId();
                var targetUserId = userId ?? currentUser;

                if (!await UserExists(targetUserId))
                {
                    return NotFound("User not found.");
                }

                if (!IsAdmin() && targetUserId != currentUser)
                {
                    return Forbid("You can only change your own email.");
                }

                await _userService.ChangeEmail(targetUserId, newEmail);

                await _loggingService.LogAsync(nameof(UserController), nameof(ChangeEmail),
                    "User email changed successfully",
                    additionalData: new BsonDocument { { "UserId", targetUserId }, { "NewEmail", newEmail } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(ChangeEmail), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromQuery] string? userId, [FromBody] string newPassword)
        {
            try
            {
                var currentUser = GetUserId();
                var targetUserId = userId ?? currentUser;

                if (!await UserExists(targetUserId))
                {
                    return NotFound("User not found.");
                }

                if (!IsAdmin() && targetUserId != currentUser)
                {
                    return Forbid("You can only change your own password.");
                }

                await _userService.ChangePassword(targetUserId, newPassword);

                await _loggingService.LogAsync(nameof(UserController), nameof(ChangePassword),
                    "User password changed successfully",
                    additionalData: new BsonDocument { { "UserId", targetUserId } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(ChangePassword), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("change-role")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ChangeRole([FromQuery] string? userId, RoleInSystem newRole)
        {
            try
            {
                var targetUserId = userId ?? GetUserId();

                if (!await UserExists(targetUserId))
                {
                    return NotFound("User not found.");
                }

                await _userService.ChangeRole(targetUserId, newRole);

                await _loggingService.LogAsync(nameof(UserController), nameof(ChangeRole),
                    "User role changed successfully",
                    additionalData: new BsonDocument { { "UserId", targetUserId }, { "NewRole", newRole.ToString() } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(ChangeRole), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUser([FromQuery] string? userId)
        {
            try
            {
                var currentUser = GetUserId();
                var targetUserId = userId ?? currentUser;

                if (!await UserExists(targetUserId))
                {
                    return NotFound("User not found.");
                }

                if (!IsAdmin() && targetUserId != currentUser)
                {
                    return Forbid("You can only delete your own account.");
                }

                await _userService.DeleteById(targetUserId);

                await _loggingService.LogAsync(nameof(UserController), nameof(DeleteUser),
                    "User deleted successfully",
                    additionalData: new BsonDocument { { "UserId", targetUserId } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(UserController),
                    nameof(DeleteUser), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
