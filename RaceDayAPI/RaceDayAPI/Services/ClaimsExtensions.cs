using System.Security.Claims;

namespace RaceDayAPI.Services
{
    public static class ClaimsExtensions
    {
        /// <summary>The logged-in user's id from the auth cookie, or null if there isn't one.</summary>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        public static bool IsOrganiser(this ClaimsPrincipal user) => user.IsInRole("Organiser");
        public static bool IsParticipantRole(this ClaimsPrincipal user) => user.IsInRole("Participant");
    }
}
