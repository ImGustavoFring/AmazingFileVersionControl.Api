using AmazingFileVersionControl.Core.DTOs.LoggingDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace AmazingFileVersionControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class LogController : ControllerBase
    {
        private readonly ILoggingService _loggingService;

        public LogController(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetLogById(string id)
        {
            try
            {
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

                return Ok(log);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(LogController),
                    nameof(GetLogById), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetLogs([FromQuery] LogFilterDTO filter)
        {
            try
            {
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

                return Ok(logs);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(LogController),
                    nameof(GetLogs), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteLogById(string id)
        {
            try
            {
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

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLogs([FromQuery] LogFilterDTO filter)
        {
            try
            {
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
