using AmazingFileVersionControl.Core.DTOs.FileDTOs;
using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadDTO request)
        {
            try
            {
                using var stream = request.File.OpenReadStream();
                var objectId = await _fileService.UploadFileAsync(
                    request.Name,
                    request.Owner,
                    request.Project,
                    request.Type,
                    stream,
                    request.Description);

                return Ok(new { FileId = objectId.ToString() });
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
                var stream = await _fileService.DownloadFileAsync(
                    request.Name,
                    request.Owner,
                    request.Project,
                    request.Version);

                return File(stream, "application/octet-stream", request.Name);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("file-info")]
        public async Task<IActionResult> GetFileInfo([FromQuery] FileQueryDTO request)
        {
            try
            {
                if (request.Version < 0)
                {
                    var filesInfo = await _fileService.GetFileInfoAsync(
                        request.Name,
                        request.Owner,
                        request.Project);

                    return Ok(filesInfo.ToJson());
                }
                else
                {
                    var fileInfo = await _fileService.GetFileInfoByVersionAsync(
                        request.Name,
                        request.Owner,
                        request.Project,
                        request.Version);

                    return Ok(fileInfo.ToJson());
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("all-info")]
        public async Task<IActionResult> GetAllFileInfo([FromQuery] string owner)
        {
            try
            {
                var filesInfo = await _fileService.GetAllOwnerFilesInfoAsync(owner);
                return Ok(filesInfo.ToJson());
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-info")]
        public async Task<IActionResult> UpdateInfoFile([FromBody] FileUpdateDTO request)
        {
            try
            {
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

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-all-info")]
        public async Task<IActionResult> UpdateAllOwnerFilesInfo([FromBody] UpdateAllFilesDTO request)
        {
            try
            {
                var updatedMetadata = BsonDocument.Parse(request.UpdatedMetadata);
                await _fileService.UpdateAllOwnerFilesInfoAsync(request.Owner, updatedMetadata);
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

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllOwnerFiles([FromQuery] string owner)
        {
            try
            {
                await _fileService.DeleteAllOwnerFilesAsync(owner);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
