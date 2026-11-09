using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Streams images out of the private Azure Blob container. The MVC app calls this endpoint
    /// and never talks to Azure itself.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IBlobStorageService _blobs;

        public FilesController(IBlobStorageService blobs)
        {
            _blobs = blobs;
        }

        // GET: api/Files/events/abc123.jpg
        [HttpGet("{**blobName}")]
        public async Task<IActionResult> GetFile(string blobName, CancellationToken ct)
        {
            // Only the two image folders this API writes to are readable.
            if (string.IsNullOrWhiteSpace(blobName) ||
                blobName.Contains("..") ||
                !(blobName.StartsWith("events/") || blobName.StartsWith("profiles/")))
            {
                return NotFound();
            }

            var file = await _blobs.DownloadAsync(blobName, ct);
            if (file == null) return NotFound();

            Response.Headers.CacheControl = "private, max-age=3600";
            return File(file.Content, file.ContentType);
        }
    }
}
