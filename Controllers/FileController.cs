using AmazingFileVersionControl.Core.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AmazingFileVersionControl.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(string name, string owner, string type, string project, IFormFile file, string? description = null, long? version = null)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    var fileId = await _fileService.UploadFileAsync(name, owner, type, project, stream, description, version);
                    return Ok(fileId);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile(string name, string owner, string type, string project, long? version = null)
        {
            try
            {
                var (stream, metadata) = await _fileService.DownloadFileWithMetadataAsync(name, owner, type, project, version);

                var content = new MultipartContent();

                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = name
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
                    FileDownloadName = name
                };
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("info/version")]
        public async Task<IActionResult> GetFileInfoByVersion(string name, string owner, string type, string project, long version)
        {
            try
            {
                var fileInfo = await _fileService.GetFileInfoByVersionAsync(name, owner, type, project, version);
                return Ok(fileInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetFileInfo(string name, string owner, string type, string project)
        {
            try
            {
                var filesInfo = await _fileService.GetFileInfoAsync(name, owner, type, project);
                return Ok(filesInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("project/info")]
        public async Task<IActionResult> GetProjectFilesInfo(string owner, string project)
        {
            try
            {
                var projectFilesInfo = await _fileService.GetProjectFilesInfoAsync(owner, project);
                return Ok(projectFilesInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("all/info")]
        public async Task<IActionResult> GetAllFilesInfo(string owner)
        {
            try
            {
                var allFilesInfo = await _fileService.GetAllFilesInfoAsync(owner);
                return Ok(allFilesInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/version")]
        public async Task<IActionResult> UpdateFileInfoByVersion(string name, string owner, string type, string project, long version, [FromBody] string updatedMetadataJson)
        {
            try
            {
                var updatedMetadata = BsonDocument.Parse(updatedMetadataJson);
                await _fileService.UpdateFileInfoByVersionAsync(name, owner, type, project, version, updatedMetadata);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateFileInfo(string name, string owner, string type, string project, [FromBody] string updatedMetadataJson)
        {
            try
            {
                var updatedMetadata = BsonDocument.Parse(updatedMetadataJson);
                await _fileService.UpdateFileInfoAsync(name, owner, type, project, updatedMetadata);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/project")]
        public async Task<IActionResult> UpdateFileInfoByProject(string owner, string project, [FromBody] string updatedMetadataJson)
        {
            try
            {
                var updatedMetadata = BsonDocument.Parse(updatedMetadataJson);
                await _fileService.UpdateFileInfoByProjectAsync(owner, project, updatedMetadata);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/all")]
        public async Task<IActionResult> UpdateAllFilesInfo(string owner, [FromBody] string updatedMetadataJson)
        {
            try
            {
                var updatedMetadata = BsonDocument.Parse(updatedMetadataJson);
                await _fileService.UpdateAllFilesInfoAsync(owner, updatedMetadata);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/version")]
        public async Task<IActionResult> DeleteFileByVersion(string name, string owner, string type, string project, long version)
        {
            try
            {
                await _fileService.DeleteFileByVersionAsync(name, owner, type, project, version);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile(string name, string owner, string type, string project)
        {
            try
            {
                await _fileService.DeleteFileAsync(name, owner, type, project);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/project")]
        public async Task<IActionResult> DeleteProjectFiles(string owner, string project)
        {
            try
            {
                await _fileService.DeleteProjectFilesAsync(owner, project);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/all")]
        public async Task<IActionResult> DeleteAllFiles(string owner)
        {
            try
            {
                await _fileService.DeleteAllFilesAsync(owner);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
