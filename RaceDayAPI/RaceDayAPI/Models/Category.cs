using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models
{
    public class Category
    {
        public int CategoryId { get; set; } // Primary key
        public int EventId { get; set; }

        [Required]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        public decimal Distance { get; set; }

        [Range(0, 100000, ErrorMessage = "The entry fee can't be negative.")]
        public decimal EntryFee { get; set; }

        [Required]
        public string AgeGroup { get; set; } = string.Empty;

        // Relationships
        public Event Event { get; set; } = null!;   // FK to Event
        public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    }
}
