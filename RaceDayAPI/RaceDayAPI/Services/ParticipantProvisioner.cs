using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;

namespace RaceDayAPI.Services
{
    /// <summary>
    /// Every user with the Participant role has exactly one Participant profile row.
    /// This creates it on first use (registration, or first login for accounts created in Part 2).
    /// </summary>
    public static class ParticipantProvisioner
    {
        public static async Task<Participant> EnsureParticipantAsync(
            RaceDayDbContext context, User user, DateTime? dateOfBirth = null, string? gender = null)
        {
            var participant = await context.Participants.FirstOrDefaultAsync(p => p.UserId == user.UserId);
            if (participant != null) return participant;

            participant = new Participant
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = dateOfBirth ?? DateTime.UtcNow.Date.AddYears(-18),
                Gender = gender ?? string.Empty,
                Address = string.Empty,
                EmergencyContact = string.Empty
            };

            context.Participants.Add(participant);
            await context.SaveChangesAsync();
            return participant;
        }
    }
}
