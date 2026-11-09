namespace RaceDayAPI.Models
{
    public class User
    {
        public int UserId { get; set; } // Primary key
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // hashed password
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Organiser or Participant
        public DateTime CreatedAt { get; set; }
        public string? ProfilePictureName { get; set; }      // blob name in Azure Blob Storage
    }
}
