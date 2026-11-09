using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDayMVC.Infrastructure
{
    /// <summary>Small formatting helpers shared by the views.</summary>
    public static class ViewHelpers
    {
        /// <summary>URL of an image served through this app (which fetches it from the API). Null when there is no image.</summary>
        public static string? ImageUrl(this IUrlHelper url, string? blobName) =>
            string.IsNullOrWhiteSpace(blobName) ? null : url.Content("~/Files/" + blobName);

        public static string Money(decimal amount) =>
            amount == 0 ? "Free" : "R " + amount.ToString("N2", CultureInfo.InvariantCulture);

        public static string Km(decimal distance) =>
            distance.ToString("0.##", CultureInfo.InvariantCulture) + " km";

        /// <summary>1 becomes "1st", 22 becomes "22nd", 113 becomes "113th".</summary>
        public static string Ordinal(int number)
        {
            var lastTwo = number % 100;
            if (lastTwo is >= 11 and <= 13) return number + "th";
            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th"
            };
        }

        public static string FinishTime(TimeSpan time) =>
            time.TotalHours >= 1
                ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";

        public static string TimeOfDay(TimeSpan? time) =>
            time.HasValue ? DateTime.Today.Add(time.Value).ToString("HH:mm", CultureInfo.InvariantCulture) : "";

        public static string ShortDate(DateTime date) =>
            date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        public static string LongDate(DateTime date) =>
            date.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
    }
}
