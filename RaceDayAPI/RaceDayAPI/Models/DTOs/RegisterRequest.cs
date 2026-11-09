namespace RaceDayAPI.Models.DTOs
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"

        /// <summary>Required when Role is "Organiser". Checked against the configured access code.</summary>
        public string? OrganiserAccessCode { get; set; }

        // Participant-only details (used to create the participant profile)
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }
}
