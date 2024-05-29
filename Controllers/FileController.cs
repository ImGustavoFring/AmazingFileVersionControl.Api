using AmazingFileVersionControl.Core.DTOs.FileDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AmazingFileVersionControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILoggingService _loggingService;
        private readonly IUserService _userService;

        public FileController(IFileService fileService, ILoggingService loggingService, IUserService userService)
        {
            _fileService = fileService;
            _loggingService = loggingService;
            _userService = userService;
        }

        private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value;
        private bool IsAdmin() => User.IsInRole("ADMIN");

        private string GetOwner(string requestedOwner)
        {
            var userLogin = GetUserLogin();
            return string.IsNullOrEmpty(requestedOwner) ? userLogin : requestedOwner;
        }

        private bool IsAuthorized(string owner)
        {
            return IsAdmin() || owner == GetUserLogin();
        }

        private async Task<bool> UserExists(string userLogin)
        {
            var user = await _userService.GetByLogin(userLogin);
            return user != null;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only upload files as yourself.");
                }

                using var stream = request.File.OpenReadStream();
                var fileId = await _fileService.UploadFileAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    stream,
                    request.Description,
                    request.Version);

                await _loggingService.LogAsync(nameof(FileController), nameof(UploadFile),
                    "File uploaded successfully",
                    additionalData: new BsonDocument { { "FileId", fileId.ToString() }, { "Owner", owner } });

                return Ok(new { FileId = fileId.ToString() });
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UploadFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFileWithMetadata([FromQuery] FileQueryWithVersionDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only download files you own.");
                }

                var (stream, fileInfo) = await _fileService.DownloadFileWithMetadataAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version.GetValueOrDefault(-1));

                var fileStreamResult = new FileStreamResult(stream, "application/octet-stream")
                {
                    FileDownloadName = request.Name
                };

                Response.Headers.Add("File-Metadata", fileInfo.ToJson());

                await _loggingService.LogAsync(nameof(FileController), nameof(DownloadFileWithMetadata),
                    "File downloaded successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return fileStreamResult;
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DownloadFileWithMetadata), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("info/version")]
        public async Task<IActionResult> GetFileInfoByVersion([FromQuery] FileQueryWithVersionDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only view information about your own files.");
                }

                var fileInfo = await _fileService.GetFileInfoByVersionAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version.GetValueOrDefault(-1));

                await _loggingService.LogAsync(nameof(FileController), nameof(GetFileInfoByVersion),
                    "File info retrieved successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return Ok(fileInfo.ToBsonDocument().ToJson());
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetFileInfoByVersion), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetFileInfo([FromQuery] FileQueryDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only view information about your own files.");
                }

                var filesInfo = await _fileService.GetFileInfoAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project);

                await _loggingService.LogAsync(nameof(FileController), nameof(GetFileInfo),
                    "Files info retrieved successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return Ok(filesInfo.Select(f => f.ToBsonDocument().ToJson()).ToList());
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetFileInfo), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("project/info")]
        public async Task<IActionResult> GetProjectFilesInfo([FromQuery] string project, [FromQuery] string? owner = null)
        {
            try
            {
                owner = GetOwner(owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only view information about your own project files.");
                }

                var projectFilesInfo = await _fileService.GetProjectFilesInfoAsync(owner, project);

                await _loggingService.LogAsync(nameof(FileController), nameof(GetProjectFilesInfo),
                    "Project files info retrieved successfully",
                    additionalData: new BsonDocument { { "Project", project }, { "Owner", owner } });

                return Ok(projectFilesInfo.Select(f => f.ToBsonDocument().ToJson()).ToList());
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetProjectFilesInfo), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("all/info")]
        public async Task<IActionResult> GetAllFilesInfo([FromQuery] string? owner = null)
        {
            try
            {
                owner = GetOwner(owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only view your own files.");
                }

                var allFilesInfo = await _fileService.GetAllFilesInfoAsync(owner);

                await _loggingService.LogAsync(nameof(FileController), nameof(GetAllFilesInfo),
                    "All files info retrieved successfully",
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok(allFilesInfo.Select(f => f.ToBsonDocument().ToJson()).ToList());
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetAllFilesInfo), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update/version")]
        public async Task<IActionResult> UpdateFileInfoByVersion([FromBody] FileUpdateWithVersionDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateFileInfoByVersionAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version.GetValueOrDefault(-1),
                    updatedMetadata);

                await _loggingService.LogAsync(nameof(FileController), nameof(UpdateFileInfoByVersion),
                    "File info updated by version successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateFileInfoByVersion), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateFileInfo([FromBody] FileUpdateDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateFileInfoAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    updatedMetadata);

                await _loggingService.LogAsync(nameof(FileController), nameof(UpdateFileInfo),
                    "File info updated successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateFileInfo), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update/project")]
        public async Task<IActionResult> UpdateFileInfoByProject([FromBody] UpdateAllFilesInProjectDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateFileInfoByProjectAsync(owner, request.Project, updatedMetadata);

                await _loggingService.LogAsync(nameof(FileController), nameof(UpdateFileInfoByProject),
                    "Project file info updated successfully",
                    additionalData: new BsonDocument { { "Project", request.Project }, { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateFileInfoByProject), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update/all")]
        public async Task<IActionResult> UpdateAllFilesInfo([FromBody] UpdateAllFilesDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateAllFilesInfoAsync(owner, updatedMetadata);

                await _loggingService.LogAsync(nameof(FileController), nameof(UpdateAllFilesInfo),
                    "All files info updated successfully",
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateAllFilesInfo), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/version")]
        public async Task<IActionResult> DeleteFileByVersion([FromQuery] FileQueryWithVersionDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteFileByVersionAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version.GetValueOrDefault(-1));

                await _loggingService.LogAsync(nameof(FileController), nameof(DeleteFileByVersion),
                    "File deleted by version successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteFileByVersion), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var owner = GetOwner(request.Owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteFileAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project);

                await _loggingService.LogAsync(nameof(FileController), nameof(DeleteFile),
                    "File deleted successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/project")]
        public async Task<IActionResult> DeleteProjectFiles([FromQuery] string project, [FromQuery] string? owner = null)
        {
            try
            {
                owner = GetOwner(owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only delete your own project files.");
                }

                await _fileService.DeleteProjectFilesAsync(owner, project);

                await _loggingService.LogAsync(nameof(FileController), nameof(DeleteProjectFiles),
                    "Project files deleted successfully",
                    additionalData: new BsonDocument { { "Project", project }, { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteProjectFiles), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/all")]
        public async Task<IActionResult> DeleteAllFiles([FromQuery] string? owner = null)
        {
            try
            {
                owner = GetOwner(owner);

                if (!await UserExists(owner))
                {
                    return NotFound("User not found.");
                }

                if (!IsAuthorized(owner))
                {
                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteAllFilesAsync(owner);

                await _loggingService.LogAsync(nameof(FileController), nameof(DeleteAllFiles),
                    "All files deleted successfully",
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteAllFiles), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
