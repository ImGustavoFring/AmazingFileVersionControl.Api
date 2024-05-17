using AmazingFileVersionControl.Core.DTOs.FileDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Net.Http.Headers;
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

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value;
        private bool IsAdmin() => User.IsInRole("ADMIN");

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
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

                return Ok(new { FileId = fileId.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only download your own files.");
                }

                var (stream, metadata) = await _fileService.DownloadFileWithMetadataAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version);

                var content = new MultipartContent();

                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = request.Name
                };
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var metadataContent = new StringContent(metadata.ToJson());
                metadataContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "metadata"
                };
                metadataContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                content.Add(fileContent);
                content.Add(metadataContent);

                return new FileStreamResult(await content.ReadAsStreamAsync(), "multipart/form-data")
                {
                    FileDownloadName = request.Name
                };
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("info/version")]
        public async Task<IActionResult> GetFileInfoByVersion([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only view information about your own files.");
                }

                var fileInfo = await _fileService.GetFileInfoByVersionAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version.GetValueOrDefault(-1));

                return Ok(fileInfo.ToBsonDocument().ToJson());
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetFileInfo([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only view information about your own files.");
                }

                var filesInfo = await _fileService.GetFileInfoAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project);

                return Ok(filesInfo.Select(f => f.ToBsonDocument().ToJson()).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("project/info")]
        public async Task<IActionResult> GetProjectFilesInfo([FromQuery] string owner, [FromQuery] string project)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only view information about your own project files.");
                }

                var projectFilesInfo = await _fileService.GetProjectFilesInfoAsync(owner, project);

                return Ok(projectFilesInfo.Select(f => f.ToBsonDocument().ToJson()).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("all/info")]
        public async Task<IActionResult> GetAllFilesInfo([FromQuery] string owner)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only view your own files.");
                }

                var allFilesInfo = await _fileService.GetAllFilesInfoAsync(owner);

                return Ok(allFilesInfo.Select(f => f.ToBsonDocument().ToJson()).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update/version")]
        public async Task<IActionResult> UpdateFileInfoByVersion([FromBody] FileUpdateDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
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

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateFileInfo([FromBody] FileUpdateDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
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

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update/project")]
        public async Task<IActionResult> UpdateFileInfoByProject([FromBody] UpdateAllFilesDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateFileInfoByProjectAsync(owner, request.Project, updatedMetadata);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update/all")]
        public async Task<IActionResult> UpdateAllFilesInfo([FromBody] UpdateAllFilesDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only update your own files.");
                }

                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateAllFilesInfoAsync(owner, updatedMetadata);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/version")]
        public async Task<IActionResult> DeleteFileByVersion([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteFileByVersionAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project,
                    request.Version.GetValueOrDefault(-1));

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] FileQueryDTO request)
        {
            try
            {
                var userLogin = GetUserLogin();
                var owner = string.IsNullOrEmpty(request.Owner) ? userLogin : request.Owner;

                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteFileAsync(
                    request.Name,
                    owner,
                    request.Type,
                    request.Project);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/project")]
        public async Task<IActionResult> DeleteProjectFiles([FromQuery] string owner, [FromQuery] string project)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only delete your own project files.");
                }

                await _fileService.DeleteProjectFilesAsync(owner, project);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete/all")]
        public async Task<IActionResult> DeleteAllFiles([FromQuery] string owner)
        {
            try
            {
                var userLogin = GetUserLogin();
                if (!IsAdmin() && owner != userLogin)
                {
                    return Forbid("You can only delete your own files.");
                }

                await _fileService.DeleteAllFilesAsync(owner);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
namespace AmazingFileVersionControl.Core.DTOs.FileDTOs
{
    public class FileUploadDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Project { get; set; }
        public IFormFile File { get; set; }
        public string? Description { get; set; }
        public string? Owner { get; set; }
        public long? Version { get; set; }
    }

    public class FileQueryDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Project { get; set; }
        public long? Version { get; set; }
        public string? Owner { get; set; }
    }

    public class FileUpdateDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Project { get; set; }
        public string UpdatedMetadata { get; set; }
        public long? Version { get; set; }
        public string? Owner { get; set; }
    }

    public class UpdateAllFilesDTO
    {
        public string UpdatedMetadata { get; set; }
        public string Project { get; set; }
        public string? Owner { get; set; }
    }
}
