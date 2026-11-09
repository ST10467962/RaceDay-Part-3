using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Models.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IBlobStorageService _blobs;

        public EventsController(RaceDayDbContext context, IBlobStorageService blobs)
        {
            _context = context;
            _blobs = blobs;
        }

        // GET: api/Events?search=&eventType=&minDistance=&maxDistance=&dateFrom=&dateTo=&upcoming=true&mine=false
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
            string? search = null,
            string? eventType = null,
            decimal? minDistance = null,
            decimal? maxDistance = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool upcoming = false,
            bool mine = false)
        {
            var query = _context.Events.AsQueryable();

            if (mine)
            {
                var userId = User.GetUserId();
                query = query.Where(e => e.OrganiserId == userId);
            }

            if (upcoming)
            {
                var today = DateTime.Today;
                query = query.Where(e => e.EventDate >= today);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(e => e.EventName.Contains(term) ||
                                         e.Location.Contains(term) ||
                                         e.Description.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.EventType == eventType);

            if (minDistance.HasValue)
                query = query.Where(e => e.Distance >= minDistance.Value);

            if (maxDistance.HasValue)
                query = query.Where(e => e.Distance <= maxDistance.Value);

            if (dateFrom.HasValue)
            {
                var fromDate = dateFrom.Value.Date;
                query = query.Where(e => e.EventDate >= fromDate);
            }

            if (dateTo.HasValue)
            {
                var toDate = dateTo.Value.Date.AddDays(1);
                query = query.Where(e => e.EventDate < toDate);
            }

            query = upcoming
                ? query.OrderBy(e => e.EventDate)
                : query.OrderByDescending(e => e.EventDate);

            return await ProjectToDto(query, User.GetUserId()).ToListAsync();
        }

        // GET: api/Events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDto>> GetEvent(int id)
        {
            var userId = User.GetUserId();
            var dto = await ProjectToDto(_context.Events.Where(e => e.EventId == id), userId).FirstOrDefaultAsync();
            if (dto == null) return NotFound();

            if (User.IsParticipantRole())
            {
                dto.MyEntry = await EntriesController.ProjectToDto(_context.Entries.Where(e =>
                        e.Participant.UserId == userId &&
                        e.Category.EventId == id &&
                        e.Status != "Cancelled"))
                    .FirstOrDefaultAsync();

                dto.CanEnrol = dto.IsUpcoming && dto.MyEntry == null && dto.Categories.Count > 0;
            }

            return dto;
        }

        // POST: api/Events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<EventDto>> PostEvent([FromBody] EventRequest request)
        {
            var problem = ValidateTimes(request);
            if (problem != null) return BadRequest(new { message = problem });

            var userId = User.GetUserId();
            if (!userId.HasValue) return Unauthorized();

            var @event = new Event
            {
                OrganiserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            Apply(@event, request);

            _context.Events.Add(@event);
            await _context.SaveChangesAsync();

            var dto = await ProjectToDto(_context.Events.Where(e => e.EventId == @event.EventId), userId).FirstAsync();
            return CreatedAtAction(nameof(GetEvent), new { id = @event.EventId }, dto);
        }

        // PUT: api/Events/5
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEvent(int id, [FromBody] EventRequest request)
        {
            var problem = ValidateTimes(request);
            if (problem != null) return BadRequest(new { message = problem });

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();
            if (@event.OrganiserId != User.GetUserId()) return Forbid();

            Apply(@event, request);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Events/5/banner   (multipart/form-data, field name "file")
        [Authorize(Roles = "Organiser")]
        [HttpPost("{id}/banner")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadBanner(int id, [FromForm] IFormFile file, CancellationToken ct)
        {
            var @event = await _context.Events.FindAsync(new object[] { id }, ct);
            if (@event == null) return NotFound();
            if (@event.OrganiserId != User.GetUserId()) return Forbid();

            var problem = BlobStorageService.Validate(file);
            if (problem != null) return BadRequest(new { message = problem });

            var oldName = @event.BannerImageName;
            @event.BannerImageName = await _blobs.UploadImageAsync(file, "events", ct);
            await _context.SaveChangesAsync(ct);
            await _blobs.DeleteAsync(oldName, ct);

            return Ok(new { bannerImageName = @event.BannerImageName });
        }

        // DELETE: api/Events/5
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();
            if (@event.OrganiserId != User.GetUserId()) return Forbid();

            // Results use Restrict on EventId (SQL Server cascade-path rule), so remove them first.
            var results = await _context.Results.Where(r => r.EventId == id).ToListAsync();
            _context.Results.RemoveRange(results);

            var bannerName = @event.BannerImageName;
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();
            await _blobs.DeleteAsync(bannerName);

            return NoContent();
        }

        // ---------- helpers ----------

        private static string? ValidateTimes(EventRequest request)
        {
            if (request.StartTime.HasValue && request.EndTime.HasValue && request.EndTime <= request.StartTime)
                return "The end time must be after the start time.";
            return null;
        }

        private static void Apply(Event @event, EventRequest request)
        {
            @event.EventName = request.EventName.Trim();
            @event.EventDate = request.EventDate.Date;
            @event.Location = request.Location.Trim();
            @event.Description = request.Description.Trim();
            @event.StartTime = request.StartTime;
            @event.EndTime = request.EndTime;
            @event.Distance = request.Distance;
            @event.EventType = request.EventType.Trim();
        }

        private static IQueryable<EventDto> ProjectToDto(IQueryable<Event> query, int? currentUserId)
        {
            var today = DateTime.Today;

            return query.Select(e => new EventDto
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                Description = e.Description,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Distance = e.Distance,
                EventType = e.EventType,
                BannerImageName = e.BannerImageName,
                CreatedAt = e.CreatedAt,
                IsUpcoming = e.EventDate >= today,
                HasTakenPlace = e.EventDate <= today,
                IsOwner = currentUserId != null && e.OrganiserId == currentUserId,
                EnrolmentCount = e.Categories.SelectMany(c => c.Entries).Count(en => en.Status != "Cancelled"),
                PendingCount = e.Categories.SelectMany(c => c.Entries).Count(en => en.Status == "Pending"),
                Categories = e.Categories
                    .OrderBy(c => c.Distance)
                    .Select(c => new CategoryDto
                    {
                        CategoryId = c.CategoryId,
                        EventId = c.EventId,
                        CategoryName = c.CategoryName,
                        Distance = c.Distance,
                        EntryFee = c.EntryFee,
                        AgeGroup = c.AgeGroup,
                        EnrolmentCount = c.Entries.Count(en => en.Status != "Cancelled")
                    })
                    .ToList()
            });
        }
    }
}
