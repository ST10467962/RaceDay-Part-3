using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;

namespace RaceDayMVC.Controllers
{
    /// <summary>
    /// Serves event banners and profile pictures. The image is fetched from the API,
    /// which is the only part of the system that talks to Azure Blob Storage.
    /// </summary>
    [SessionAuthorize]
    public class FilesController : Controller
    {
        // Names the API generates: events/<32 hex>.<ext> or profiles/<32 hex>.<ext>.
        private static readonly Regex BlobNamePattern =
            new(@"^(events|profiles)/[0-9a-f]{32}\.(jpe?g|png|gif|webp)$", RegexOptions.CultureInvariant);

        private readonly ApiClient _api;

        public FilesController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet("Files/{**blobName}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Get(string blobName)
        {
            if (!BlobNamePattern.IsMatch(blobName ?? string.Empty)) return NotFound();

            try
            {
                var file = await _api.GetFileAsync(blobName!);
                if (file == null) return NotFound();

                return File(file.Value.Bytes, file.Value.ContentType);
            }
            catch (ApiUnauthorizedException)
            {
                // The login has expired. Send the visitor to log in again.
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }
            catch (ApiException)
            {
                return StatusCode(StatusCodes.Status502BadGateway);
            }
        }
    }
}
