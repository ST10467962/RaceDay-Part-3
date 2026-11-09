using System.Text.Json;
using RaceDayMVC.Models.Api;

namespace RaceDayMVC.Infrastructure
{
    public static class SessionKeys
    {
        public const string User = "RaceDay.User";
        public const string ApiCookies = "RaceDay.ApiCookies";
    }

    /// <summary>What the MVC app remembers about the logged-in user between requests.</summary>
    public class SessionUser
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? ParticipantId { get; set; }
        public string? ProfilePictureName { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool IsOrganiser => Role == "Organiser";
        public bool IsParticipant => Role == "Participant";

        public string Initials
        {
            get
            {
                var first = string.IsNullOrEmpty(FirstName) ? "" : FirstName[..1];
                var last = string.IsNullOrEmpty(LastName) ? "" : LastName[..1];
                return (first + last).ToUpperInvariant();
            }
        }

        public static SessionUser From(UserDto dto) => new()
        {
            UserId = dto.UserId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = dto.Role,
            ParticipantId = dto.ParticipantId,
            ProfilePictureName = dto.ProfilePictureName
        };
    }

    public static class UserSessionExtensions
    {
        public static SessionUser? GetUser(this ISession session)
        {
            var json = session.GetString(SessionKeys.User);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<SessionUser>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static void SetUser(this ISession session, SessionUser user)
        {
            session.SetString(SessionKeys.User, JsonSerializer.Serialize(user));
        }
    }
}
