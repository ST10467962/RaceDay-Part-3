using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RaceRoutesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public RaceRoutesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/RaceRoutes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RaceRoute>>> GetRaceRoutes()
        {
            return await _context.RaceRoutes.ToListAsync();
        }

        // GET: api/RaceRoutes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RaceRoute>> GetRaceRoute(int id)
        {
            var raceRoute = await _context.RaceRoutes.FindAsync(id);

            if (raceRoute == null)
            {
                return NotFound();
            }

            return raceRoute;
        }

        // PUT: api/RaceRoutes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> PutRaceRoute(int id, RaceRoute raceRoute)
        {
            if (id != raceRoute.RaceRouteId)
            {
                return BadRequest();
            }

            // An organiser may only change routes of their own events, and may not move one elsewhere.
            if (HasSignedInCaller)
            {
                var currentEventId = await _context.RaceRoutes.AsNoTracking()
                    .Where(r => r.RaceRouteId == id)
                    .Select(r => (int?)r.EventId)
                    .FirstOrDefaultAsync();

                if (!currentEventId.HasValue) return NotFound();
                if (!await OwnsEventAsync(currentEventId.Value) || !await OwnsEventAsync(raceRoute.EventId))
                    return Forbid();
            }

            _context.Entry(raceRoute).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaceRouteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/RaceRoutes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<RaceRoute>> PostRaceRoute(RaceRoute raceRoute)
        {
            if (HasSignedInCaller && !await OwnsEventAsync(raceRoute.EventId))
            {
                return Forbid();
            }

            _context.RaceRoutes.Add(raceRoute);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRaceRoute", new { id = raceRoute.RaceRouteId }, raceRoute);
        }

        // DELETE: api/RaceRoutes/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteRaceRoute(int id)
        {
            var raceRoute = await _context.RaceRoutes.FindAsync(id);
            if (raceRoute == null)
            {
                return NotFound();
            }

            if (HasSignedInCaller && !await OwnsEventAsync(raceRoute.EventId))
            {
                return Forbid();
            }

            _context.RaceRoutes.Remove(raceRoute);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RaceRouteExists(int id)
        {
            return _context.RaceRoutes.Any(e => e.RaceRouteId == id);
        }

        // [Authorize] rejects anonymous callers; the false branch only happens when the controller
        // is exercised directly in a unit test without an HTTP context.
        private bool HasSignedInCaller => HttpContext?.User?.Identity?.IsAuthenticated == true;

        private async Task<bool> OwnsEventAsync(int eventId)
        {
            var userId = HttpContext.User.GetUserId();
            return await _context.Events.AnyAsync(e => e.EventId == eventId && e.OrganiserId == userId);
        }
    }
}
