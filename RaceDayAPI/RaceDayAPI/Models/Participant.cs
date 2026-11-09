namespace RaceDayAPI.Models
{
    public class Participant
    {
        public int ParticipantId { get; set; } // Primary key
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;

        // Part 3: links the participant profile to the login account
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Relationships
        public ICollection<Entry> Entries { get; set; } = new List<Entry>();
        public ICollection<Result> Results { get; set; } = new List<Result>();
    }
}