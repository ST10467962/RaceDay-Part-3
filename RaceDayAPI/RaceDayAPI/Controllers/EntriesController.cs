using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Models.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// An "entry" is an enrolment of a participant in one category of an event.
    /// Participants only ever see their own entries; organisers see entries for their own events.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EntriesController : ControllerBase
    {
        private static readonly string[] ValidStatuses = { "Pending", "Confirmed", "Cancelled" };

        private readonly RaceDayDbContext _context;

        public EntriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Entries?eventId=1&participantId=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EntryDto>>> GetEntries(int? eventId = null, int? participantId = null)
        {
            var scoped = await ScopeToCurrentUserAsync(_context.Entries.AsQueryable());
            if (scoped == null) return Ok(new List<EntryDto>());

            var query = scoped;
            if (eventId.HasValue) query = query.Where(e => e.Category.EventId == eventId.Value);
            if (participantId.HasValue) query = query.Where(e => e.ParticipantId == participantId.Value);

            return await ProjectToDto(query.OrderByDescending(e => e.EntryDate)).ToListAsync();
        }

        // GET: api/Entries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EntryDto>> GetEntry(int id)
        {
            var scoped = await ScopeToCurrentUserAsync(_context.Entries.AsQueryable());
            if (scoped == null) return NotFound();

            var dto = await ProjectToDto(scoped.Where(e => e.EntryId == id)).FirstOrDefaultAsync();
            if (dto == null) return NotFound();
            return dto;
        }

        // POST: api/Entries   body: { "categoryId": 3 }
        // The participant is always the logged-in user, so nobody can enrol someone else.
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<EntryDto>> PostEntry([FromBody] EnrolRequest request)
        {
            var userId = User.GetUserId();
            var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
            if (user == null) return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
            if (category == null)
                return NotFound(new { message = "That category no longer exists." });

            if (category.Event.EventDate.Date < DateTime.Today)
                return BadRequest(new { message = "This event has already taken place, so enrolment is closed." });

            var participant = await ParticipantProvisioner.EnsureParticipantAsync(_context, user);

            var alreadyEnrolled = await _context.Entries.AnyAsync(e =>
                e.ParticipantId == participant.ParticipantId &&
                e.Category.EventId == category.EventId &&
                e.Status != "Cancelled");
            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this event." });

            var entry = new Entry
            {
                ParticipantId = participant.ParticipantId,
                CategoryId = category.CategoryId,
                EntryDate = DateTime.UtcNow,
                PaymentStatus = false,
                Status = "Pending"
            };

            _context.Entries.Add(entry);
            await _context.SaveChangesAsync();

            var dto = await ProjectToDto(_context.Entries.Where(e => e.EntryId == entry.EntryId)).FirstAsync();
            return CreatedAtAction(nameof(GetEntry), new { id = entry.EntryId }, dto);
        }

        // PUT: api/Entries/5/status   body: { "status": "Confirmed" }
        // Organisers can set any status on entries for their events.
        // Participants can only withdraw (Cancelled) their own entry.
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] EntryStatusRequest request)
        {
            var status = ValidStatuses.FirstOrDefault(s => s.Equals(request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (status == null)
                return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });

            var entry = await _context.Entries
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EntryId == id);
            if (entry == null) return NotFound();

            var userId = User.GetUserId();
            if (User.IsOrganiser())
            {
                if (entry.Category.Event.OrganiserId != userId) return Forbid();
            }
            else
            {
                if (entry.Participant.UserId != userId || status != "Cancelled") return Forbid();

                // Participants may only withdraw before the event takes place. This mirrors the
                // CanWithdraw flag the API sends to the MVC so the rule can't be bypassed by calling the API directly.
                if (entry.Category.Event.EventDate.Date < DateTime.Today)
                    return BadRequest(new { message = "This event has already taken place, so the enrolment can't be withdrawn." });
            }

            entry.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Entries/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var entry = await _context.Entries
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EntryId == id);
            if (entry == null) return NotFound();
            if (entry.Category.Event.OrganiserId != User.GetUserId()) return Forbid();

            _context.Entries.Remove(entry);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ---------- helpers ----------

        /// <summary>Limits a query to what the current user is allowed to see. Null means "nothing".</summary>
        private async Task<IQueryable<Entry>?> ScopeToCurrentUserAsync(IQueryable<Entry> query)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return null;

            if (User.IsOrganiser())
                return query.Where(e => e.Category.Event.OrganiserId == userId.Value);

            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId.Value);
            if (participant == null) return null;

            var participantId = participant.ParticipantId;
            return query.Where(e => e.ParticipantId == participantId);
        }

        internal static IQueryable<EntryDto> ProjectToDto(IQueryable<Entry> query)
        {
            var today = DateTime.Today;

            return query.Select(e => new EntryDto
            {
                EntryId = e.EntryId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                ParticipantEmail = e.Participant.User != null ? e.Participant.User.Email : null,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.CategoryName,
                EventId = e.Category.EventId,
                EventName = e.Category.Event.EventName,
                EventDate = e.Category.Event.EventDate,
                EventLocation = e.Category.Event.Location,
                EntryDate = e.EntryDate,
                PaymentStatus = e.PaymentStatus,
                Status = e.Status,
                CanWithdraw = e.Status != "Cancelled" && e.Category.Event.EventDate >= today
            });
        }
    }
}
