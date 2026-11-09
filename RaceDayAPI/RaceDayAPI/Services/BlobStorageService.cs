using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services
{
    public interface IBlobStorageService
    {
        /// <summary>Uploads an image into the given virtual folder and returns the blob name.</summary>
        Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);

        /// <summary>Downloads a blob. Returns null when the blob does not exist.</summary>
        Task<BlobFile?> DownloadAsync(string blobName, CancellationToken ct = default);

        /// <summary>Deletes a blob if it exists. A null or empty name is ignored.</summary>
        Task DeleteAsync(string? blobName, CancellationToken ct = default);
    }

    public record BlobFile(Stream Content, string ContentType);

    /// <summary>
    /// Wraps Azure Blob Storage. The container is private: images are only ever served
    /// through the API (GET api/Files/{blobName}), never straight from Azure.
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        public const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" }
        };

        private readonly BlobContainerClient _container;
        private readonly ILogger<BlobStorageService> _logger;
        private bool _containerReady;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger;

            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];
            if (string.IsNullOrWhiteSpace(containerName)) containerName = "raceday-images";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Lets the API start without Azure configured; uploads fail with a clear message instead.
                connectionString = "UseDevelopmentStorage=true";
                _logger.LogWarning("AzureBlobStorage:ConnectionString is not set. Falling back to the local Azurite emulator.");
            }

            _container = new BlobContainerClient(connectionString, containerName);
        }

        /// <summary>Returns an error message when the file is not an acceptable image, otherwise null.</summary>
        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "Please choose an image to upload.";

            if (file.Length > MaxImageBytes)
                return "The image is too large. The maximum size is 5 MB.";

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedTypes.ContainsKey(extension))
                return "Only JPG, PNG, GIF or WebP images are allowed.";

            if (string.IsNullOrEmpty(file.ContentType) ||
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return "The selected file is not an image.";

            return null;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            await EnsureContainerAsync(ct);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
            var blob = _container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = AllowedTypes[extension] }
            }, ct);

            return blobName;
        }

        public async Task<BlobFile?> DownloadAsync(string blobName, CancellationToken ct = default)
        {
            var blob = _container.GetBlobClient(blobName);
            try
            {
                var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
                return new BlobFile(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task DeleteAsync(string? blobName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobName)) return;

            try
            {
                await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                // A failed clean-up must never fail the user's request.
                _logger.LogWarning(ex, "Could not delete blob {BlobName}", blobName);
            }
        }

        private async Task EnsureContainerAsync(CancellationToken ct)
        {
            if (_containerReady) return;
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            _containerReady = true;
        }
    }
}
