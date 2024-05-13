using AmazingFileVersionControl.Core.DTOs.FileDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
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

        public FileController(IFileService fileService, ILoggingService loggingService)
        {
            _fileService = fileService;
            _loggingService = loggingService;
        }

        private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value;
        private bool IsAdmin() => User.IsInRole("ADMIN");

        [HttpPost("upload")]
        public async Task<IActionResult> UploadOwnerFile([FromForm] FileUploadDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(UploadOwnerFile), "Forbidden: Upload attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only upload files as yourself.");
                }

                using var stream = request.File.OpenReadStream();
                var objectId = await _fileService.UploadFileAsync(
                    request.Name,
                    owner,
                    request.Project,
                    request.Type,
                    stream,
                    request.Description);

                await _loggingService.LogAsync(nameof(FileController), nameof(UploadOwnerFile),
                    "File uploaded successfully",
                    additionalData: new BsonDocument {
                        { "FileId", objectId.ToString() },
                        { "Owner", owner } });

                return Ok(new { FileId = objectId.ToString() });
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UploadOwnerFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadOwnerFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(DownloadOwnerFile),
                        "Forbidden: Download attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only download your own files.");
                }

                var stream = await _fileService.DownloadFileAsync(
                    request.Name,
                    owner,
                    request.Project,
                    request.Version);

                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DownloadOwnerFile), "File downloaded successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name },
                        { "Owner", owner } });

                return File(stream, "application/octet-stream", request.Name);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DownloadOwnerFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetOwnerFileInfo([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetOwnerFileInfo), "Forbidden: File info access attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only view information about your own files.");
                }

                if (request.Version < 0)
                {
                    var filesInfo = await _fileService.GetFileInfoAsync(
                        request.Name,
                        owner,
                        request.Project);

                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetOwnerFileInfo), "File info retrieved successfully",
                        additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner } });

                    return Ok(filesInfo.ToJson());
                }
                else
                {
                    var fileInfo = await _fileService.GetFileInfoByVersionAsync(
                        request.Name,
                        owner,
                        request.Project,
                        request.Version);

                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetOwnerFileInfo), "File info by version retrieved successfully",
                        additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", owner }, { "Version", request.Version } });
                    return Ok(fileInfo.ToJson());
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController), nameof(GetOwnerFileInfo), ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("all-info")]
        public async Task<IActionResult> GetOwnerAllFileInfo([FromQuery] string owner)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetOwnerAllFileInfo),
                        "Forbidden: Access to all file info attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only view your own files.");
                }

                var filesInfo = await _fileService.GetAllOwnerFilesInfoAsync(owner);
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetOwnerAllFileInfo),
                    "All file info retrieved successfully",
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok(filesInfo.ToJson());
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetOwnerAllFileInfo),
                    ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-info")]
        public async Task<IActionResult> UpdateOwnerInfoFile([FromBody] FileUpdateDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(UpdateOwnerInfoFile),
                        "Forbidden: Update file info attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                if (request.Version < 0)
                {
                    await _fileService.UpdateFileInfoAsync(
                        request.Name,
                        owner,
                        request.Project,
                        updatedMetadata);
                }
                else
                {
                    await _fileService.UpdateFileInfoByVersionAsync(
                        request.Name,
                        owner,
                        request.Project,
                        request.Version,
                        updatedMetadata);
                }

                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateOwnerInfoFile), "File info updated successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name },
                        { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateOwnerInfoFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-all-info")]
        public async Task<IActionResult> UpdateOwnerAllFilesInfo([FromBody] UpdateAllFilesDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(UpdateOwnerAllFilesInfo),
                        "Forbidden: Update all files info attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner }, { "UserLogin", userLogin } });

                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateAllOwnerFilesInfoAsync(owner, updatedMetadata);
                await _loggingService.LogAsync(nameof(FileController), nameof(UpdateOwnerAllFilesInfo),
                    "All file info updated successfully", additionalData: new BsonDocument { { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateOwnerAllFilesInfo),
                    ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteOwnerFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(DeleteOwnerFile), "Forbidden: Delete file attempt as another user",
                        "Warning",
                        new BsonDocument { { "Owner", owner }, { "UserLogin", userLogin } });
                    return Forbid("You can only delete your own files.");
                }

                if (request.Version < 0)
                {
                    await _fileService.DeleteFileAsync(
                        request.Name,
                        owner,
                        request.Project);
                }
                else
                {
                    await _fileService.DeleteFileByVersionAsync(
                        request.Name,
                        owner,
                        request.Project,
                        request.Version);
                }

                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteOwnerFile), "File deleted successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name },
                        { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteOwnerFile),
                    ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteOwnerAllFiles([FromQuery] string owner)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (!IsAdmin() && owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(DeleteOwnerAllFiles),
                        "Forbidden: Delete all files attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner }, { "UserLogin", userLogin } });

                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteAllOwnerFilesAsync(owner);
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteOwnerAllFiles), "All files deleted successfully",
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteOwnerAllFiles),
                    ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
