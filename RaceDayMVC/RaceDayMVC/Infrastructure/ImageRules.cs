namespace RaceDayMVC.Infrastructure
{
    /// <summary>
    /// Quick checks on an uploaded image so people get feedback before anything is sent.
    /// These are form-level checks only. The API (BlobStorageService.Validate) is the authority and applies the same limits.
    /// </summary>
    public static class ImageRules
    {
        public const long MaxBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        /// <summary>Returns a message when the image can't be used, or null when it is fine (or when no file was chosen).</summary>
        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            if (file.Length > MaxBytes)
                return "That image is larger than 5 MB. Choose a smaller one.";

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return "Use a JPG, PNG, GIF or WebP image.";

            return null;
        }

        public static bool HasFile(IFormFile? file) => file != null && file.Length > 0;
    }
}
