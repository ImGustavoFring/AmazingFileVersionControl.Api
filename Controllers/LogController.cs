/**
 * @file LogController.cs
 * @brief Контроллер для управления логами.
 */

using AmazingFileVersionControl.Core.DTOs.LoggingDTOs;
using AmazingFileVersionControl.Core.Services;
using AmazingFileVersionControl.Core.Models.LoggingEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AmazingFileVersionControl.Api.Controllers
{
    /**
     * @class LogController
     * @brief Класс контроллера для управления логами.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class LogController : ControllerBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IUserService _userService;

        /**
         * @brief Конструктор класса LogController.
         * @param loggingService Сервис логирования.
         * @param userService Сервис управления пользователями.
         */
        public LogController(ILoggingService loggingService, IUserService userService)
        {
            _loggingService = loggingService;
            _userService = userService;
        }

        /**
         * @brief Проверить, существует ли пользователь.
         * @param userId Идентификатор пользователя.
         * @return true, если пользователь существует, иначе false.
         */
        private async Task<bool> UserExists(string userId)
        {
            var user = await _userService.GetById(userId);
            return user != null;
        }

        /**
         * @brief Получить идентификатор текущего пользователя.
         * @return Идентификатор текущего пользователя.
         */
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /**
         * @brief Получить лог по идентификатору.
         * @param id Идентификатор лога.
         * @return Результат выполнения действия.
         */
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetLogById(string id)
        {
            try
            {
                var currentUser = GetUserId();

                if (!await UserExists(currentUser))
                {
                    return NotFound("User not found.");
                }

                var log = await _loggingService.GetLogByIdAsync(id);
                if (log == null)
                {
                    await _loggingService.LogAsync(nameof(LogController), nameof(GetLogById),
                        $"Log with id {id} not found", "Warning");

                    return NotFound();
                }

                await _loggingService.LogAsync(nameof(LogController), nameof(GetLogById),
                    "Log retrieved successfully",
                    additionalData: new BsonDocument { { "LogId", id } });

                var logJson = JsonConvert.SerializeObject(log, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                return Ok(logJson);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(LogController),
                    nameof(GetLogById), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        /**
         * @brief Получить список логов по фильтрам.
         * @param filter DTO с данными фильтрации логов.
         * @return Результат выполнения действия.
         */
        [HttpGet("list")]
        public async Task<IActionResult> GetLogs([FromQuery] LogFilterDTO filter)
        {
            try
            {
                var currentUser = GetUserId();

                if (!await UserExists(currentUser))
                {
                    return NotFound("User not found.");
                }

                BsonDocument additionalDataBson = null;

                if (!string.IsNullOrEmpty(filter.AdditionalData))
                {
                    additionalDataBson = BsonDocument.Parse(filter.AdditionalData);
                }

                var logs = await _loggingService.GetLogsAsync(
                    string.IsNullOrEmpty(filter.Controller) ? null : filter.Controller,
                    string.IsNullOrEmpty(filter.Action) ? null : filter.Action,
                    filter.StartDate ?? null,
                    filter.EndDate ?? null,
                    string.IsNullOrEmpty(filter.Level) ? null : filter.Level,
                    additionalDataBson);

                await _loggingService.LogAsync(nameof(LogController), nameof(GetLogs),
                    "Logs retrieved successfully");

                var logsJson = JsonConvert.SerializeObject(logs, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                return Ok(logsJson);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(LogController),
                    nameof(GetLogs), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        /**
         * @brief Удалить лог по идентификатору.
         * @param id Идентификатор лога.
         * @return Результат выполнения действия.
         */
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteLogById(string id)
        {
            try
            {
                var currentUser = GetUserId();

                if (!await UserExists(currentUser))
                {
                    return NotFound("User not found.");
                }

                await _loggingService.DeleteLogByIdAsync(id);

                await _loggingService.LogAsync(nameof(LogController), nameof(DeleteLogById),
                    "Log deleted successfully",
                    additionalData: new BsonDocument { { "LogId", id } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(LogController),
                    nameof(DeleteLogById), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        /**
         * @brief Удалить список логов по фильтрам.
         * @param filter DTO с данными фильтрации логов.
         * @return Результат выполнения действия.
         */
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLogs([FromQuery] LogFilterDTO filter)
        {
            try
            {
                var currentUser = GetUserId();

                if (!await UserExists(currentUser))
                {
                    return NotFound("User not found.");
                }

                BsonDocument additionalDataBson = null;

                if (!string.IsNullOrEmpty(filter.AdditionalData))
                {
                    additionalDataBson = BsonDocument.Parse(filter.AdditionalData);
                }

                await _loggingService.DeleteLogsAsync(
                    string.IsNullOrEmpty(filter.Controller) ? null : filter.Controller,
                    string.IsNullOrEmpty(filter.Action) ? null : filter.Action,
                    filter.StartDate ?? null,
                    filter.EndDate ?? null,
                    string.IsNullOrEmpty(filter.Level) ? null : filter.Level,
                    additionalDataBson);

                await _loggingService.LogAsync(nameof(LogController), nameof(DeleteLogs),
                    "Logs deleted successfully");

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(LogController),
                    nameof(DeleteLogs), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
