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
    /// Race results. Organisers capture finish times and finishing positions for their own events.
    /// Participants can only read their own results.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Results?eventId=1&participantId=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetResults(int? eventId = null, int? participantId = null)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Unauthorized();

            var query = _context.Results.AsQueryable();

            if (User.IsOrganiser())
            {
                query = query.Where(r => r.Event.OrganiserId == userId.Value);
            }
            else
            {
                var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId.Value);
                if (participant == null) return Ok(new List<ResultDto>());
                var ownId = participant.ParticipantId;
                query = query.Where(r => r.ParticipantId == ownId);
            }

            if (eventId.HasValue) query = query.Where(r => r.EventId == eventId.Value);
            if (participantId.HasValue) query = query.Where(r => r.ParticipantId == participantId.Value);

            return await ProjectToDto(query.OrderBy(r => r.EventId).ThenBy(r => r.Position)).ToListAsync();
        }

        // GET: api/Results/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ResultDto>> GetResult(int id)
        {
            var dto = await ProjectToDto(_context.Results.Where(r => r.ResultId == id)).FirstOrDefaultAsync();
            if (dto == null) return NotFound();

            var userId = User.GetUserId();
            if (User.IsOrganiser())
            {
                var ownsEvent = await _context.Events.AnyAsync(e => e.EventId == dto.EventId && e.OrganiserId == userId);
                if (!ownsEvent) return Forbid();
            }
            else
            {
                var isOwn = await _context.Participants.AnyAsync(p => p.ParticipantId == dto.ParticipantId && p.UserId == userId);
                if (!isOwn) return Forbid();
            }

            return dto;
        }

        // POST: api/Results/event/5
        // Body is the complete result set for the event:
        // [ { "entryId": 1, "finishTime": "00:25:31", "position": 1 }, { "entryId": 2, "finishTime": null } ]
        //
        // Rules:
        //  - An entry with a finish time must also have a position.
        //  - Positions run 1, 2, 3 ... N with no gaps or repeats.
        //  - A faster time can't sit behind a slower one (ties are allowed).
        //  - Any stored result that is missing from the body is removed.
        [HttpPost("event/{eventId}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> SaveEventResults(int eventId, [FromBody] List<ResultRequest>? requests)
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null) return NotFound();
            if (@event.OrganiserId != User.GetUserId()) return Forbid();

            if (@event.EventDate.Date > DateTime.Today)
                return BadRequest(new { message = "Results can only be captured once the event has taken place." });

            requests ??= new List<ResultRequest>();

            if (requests.Select(r => r.EntryId).Distinct().Count() != requests.Count)
                return BadRequest(new { message = "Each entry can only appear once." });

            var timed = requests.Where(r => r.FinishTime.HasValue).ToList();

            foreach (var request in timed)
            {
                if (request.FinishTime!.Value <= TimeSpan.Zero || request.FinishTime.Value >= TimeSpan.FromHours(24))
                    return BadRequest(new { message = "Finish times must be between 00:00:01 and 23:59:59." });
            }

            if (timed.Any(r => r.Position == null))
                return BadRequest(new { message = "Every finish time needs a finishing position." });

            if (timed.Any(r => r.Position!.Value < 1))
                return BadRequest(new { message = "Finishing positions start at 1." });

            var ordered = timed.OrderBy(r => r.Position!.Value).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].Position!.Value != i + 1)
                    return BadRequest(new { message = "Finishing positions must run 1, 2, 3 and so on, with no gaps or repeats." });
            }

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].FinishTime!.Value < ordered[i - 1].FinishTime!.Value)
                    return BadRequest(new { message = "A faster finish time can't have a worse position than a slower one." });
            }

            var entries = await _context.Entries
                .Where(e => e.Category.EventId == eventId)
                .ToDictionaryAsync(e => e.EntryId);

            foreach (var request in requests)
            {
                if (!entries.ContainsKey(request.EntryId))
                    return BadRequest(new { message = $"Entry {request.EntryId} does not belong to this event." });
            }

            if (timed.Any(r => entries[r.EntryId].Status == "Cancelled"))
                return BadRequest(new { message = "A result can't be recorded for a cancelled enrolment." });

            var existing = await _context.Results.Where(r => r.EventId == eventId).ToListAsync();
            var keep = timed.Select(r => r.EntryId).ToHashSet();
            _context.Results.RemoveRange(existing.Where(r => !keep.Contains(r.EntryId)));

            var existingByEntry = existing.ToDictionary(r => r.EntryId);
            foreach (var request in timed)
            {
                if (existingByEntry.TryGetValue(request.EntryId, out var result))
                {
                    result.FinishTime = request.FinishTime!.Value;
                    result.Position = request.Position!.Value;
                    result.RecordedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.Results.Add(new Result
                    {
                        EntryId = request.EntryId,
                        EventId = eventId,
                        ParticipantId = entries[request.EntryId].ParticipantId,
                        FinishTime = request.FinishTime!.Value,
                        Position = request.Position!.Value,
                        RecordedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Results saved." });
        }

        private IQueryable<ResultDto> ProjectToDto(IQueryable<Result> query)
        {
            return query.Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EntryId = r.EntryId,
                EventId = r.EventId,
                EventName = r.Event.EventName,
                EventDate = r.Event.EventDate,
                ParticipantId = r.ParticipantId,
                ParticipantName = r.Participant.FirstName + " " + r.Participant.LastName,
                CategoryName = r.Entry.Category.CategoryName,
                FinishTime = r.FinishTime,
                Position = r.Position,
                TotalFinishers = _context.Results.Count(x => x.EventId == r.EventId),
                RecordedAt = r.RecordedAt
            });
        }
    }
}
