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
    [Authorize]
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

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (request.Owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(UploadFile), "Forbidden: Upload attempt as another user",
                        "Warning", new BsonDocument { { "Owner", request.Owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only upload files as yourself.");
                }

                using var stream = request.File.OpenReadStream();
                var objectId = await _fileService.UploadFileAsync(
                    request.Name,
                    request.Owner,
                    request.Project,
                    request.Type,
                    stream,
                    request.Description);

                await _loggingService.LogAsync(nameof(FileController), nameof(UploadFile),
                    "File uploaded successfully",
                    additionalData: new BsonDocument { 
                        { "FileId", objectId.ToString() },
                        { "Owner", request.Owner } });

                return Ok(new { FileId = objectId.ToString() });
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
        public async Task<IActionResult> DownloadFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (request.Owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(DownloadFile),
                        "Forbidden: Download attempt as another user",
                        "Warning", new BsonDocument { { "Owner", request.Owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only download your own files.");
                }

                var stream = await _fileService.DownloadFileAsync(
                    request.Name,
                    request.Owner,
                    request.Project,
                    request.Version);

                await _loggingService.LogAsync(nameof(FileController), 
                    nameof(DownloadFile), "File downloaded successfully", 
                    additionalData: new BsonDocument { { "FileName", request.Name },
                        { "Owner", request.Owner } });
                
                return File(stream, "application/octet-stream", request.Name);
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DownloadFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("file-info")]
        public async Task<IActionResult> GetFileInfo([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (request.Owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetFileInfo), "Forbidden: File info access attempt as another user",
                        "Warning", new BsonDocument { { "Owner", request.Owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only view information about your own files.");
                }

                if (request.Version < 0)
                {
                    var filesInfo = await _fileService.GetFileInfoAsync(
                        request.Name,
                        request.Owner,
                        request.Project);

                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetFileInfo), "File info retrieved successfully", 
                        additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", request.Owner } });

                    return Ok(filesInfo.ToJson());
                }
                else
                {
                    var fileInfo = await _fileService.GetFileInfoByVersionAsync(
                        request.Name,
                        request.Owner,
                        request.Project,
                        request.Version);

                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetFileInfo), "File info by version retrieved successfully",
                        additionalData: new BsonDocument { { "FileName", request.Name }, { "Owner", request.Owner }, { "Version", request.Version } });
                    return Ok(fileInfo.ToJson());
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController), nameof(GetFileInfo), ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("all-info")]
        public async Task<IActionResult> GetAllFileInfo([FromQuery] string owner)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(GetAllFileInfo),
                        "Forbidden: Access to all file info attempt as another user",
                        "Warning", new BsonDocument { { "Owner", owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only view your own files.");
                }

                var filesInfo = await _fileService.GetAllOwnerFilesInfoAsync(owner);
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetAllFileInfo), 
                    "All file info retrieved successfully",
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok(filesInfo.ToJson());
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(GetAllFileInfo), 
                    ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-info")]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> UpdateInfoFile([FromBody] FileUpdateDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (request.Owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController), 
                        nameof(UpdateInfoFile), 
                        "Forbidden: Update file info attempt as another user",
                        "Warning", new BsonDocument { { "Owner", request.Owner },
                            { "UserLogin", userLogin } });

                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                if (request.Version < 0)
                {
                    await _fileService.UpdateFileInfoAsync(
                        request.Name,
                        request.Owner,
                        request.Project,
                        updatedMetadata);
                }
                else
                {
                    await _fileService.UpdateFileInfoByVersionAsync(
                        request.Name,
                        request.Owner,
                        request.Project,
                        request.Version,
                        updatedMetadata);
                }

                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateInfoFile), "File info updated successfully",
                    additionalData: new BsonDocument { { "FileName", request.Name },
                        { "Owner", request.Owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController),
                    nameof(UpdateInfoFile), ex.Message, "Error",
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-all-info")]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> UpdateAllOwnerFilesInfo([FromBody] UpdateAllFilesDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (request.Owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(UpdateAllOwnerFilesInfo), "Forbidden: Update all files info attempt as another user",
                        "Warning", new BsonDocument { { "Owner", request.Owner }, { "UserLogin", userLogin } });

                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateAllOwnerFilesInfoAsync(request.Owner, updatedMetadata);
                await _loggingService.LogAsync(nameof(FileController), nameof(UpdateAllOwnerFilesInfo),
                    "All file info updated successfully", additionalData: new BsonDocument { { "Owner", request.Owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController), 
                    nameof(UpdateAllOwnerFilesInfo),
                    ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> DeleteFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (request.Owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(DeleteFile), "Forbidden: Delete file attempt as another user",
                        "Warning", 
                        new BsonDocument { { "Owner", request.Owner }, { "UserLogin", userLogin } });
                    return Forbid("You can only delete your own files.");
                }

                if (request.Version < 0)
                {
                    await _fileService.DeleteFileAsync(
                        request.Name,
                        request.Owner,
                        request.Project);
                }
                else
                {
                    await _fileService.DeleteFileByVersionAsync(
                        request.Name,
                        request.Owner,
                        request.Project,
                        request.Version);
                }

                await _loggingService.LogAsync(nameof(FileController),
                    nameof(DeleteFile), "File deleted successfully", 
                    additionalData: new BsonDocument { { "FileName", request.Name },
                        { "Owner", request.Owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController), 
                    nameof(DeleteFile),
                    ex.Message, "Error", new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete-all")]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> DeleteAllOwnerFiles([FromQuery] string owner)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (owner != userLogin)
                {
                    await _loggingService.LogAsync(nameof(FileController),
                        nameof(DeleteAllOwnerFiles), 
                        "Forbidden: Delete all files attempt as another user", 
                        "Warning", new BsonDocument { { "Owner", owner }, { "UserLogin", userLogin } });

                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteAllOwnerFilesAsync(owner);
                await _loggingService.LogAsync(nameof(FileController), 
                    nameof(DeleteAllOwnerFiles), "All files deleted successfully", 
                    additionalData: new BsonDocument { { "Owner", owner } });

                return Ok();
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync(nameof(FileController), 
                    nameof(DeleteAllOwnerFiles), 
                    ex.Message, "Error", 
                    new BsonDocument { { "Exception", ex.ToString() } });

                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
