using Microsoft.AspNetCore.Authorization;
using RaceDayAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories(int? eventId = null)
        {
            var query = _context.Categories.AsQueryable();
            if (eventId.HasValue)
            {
                query = query.Where(c => c.EventId == eventId.Value);
            }
            return await query.ToListAsync();
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // PUT: api/Categories/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest();
            }

            // An organiser may only change categories of their own events, and may not move one elsewhere.
            if (HasSignedInCaller)
            {
                var currentEventId = await _context.Categories.AsNoTracking()
                    .Where(c => c.CategoryId == id)
                    .Select(c => c.EventId)
                    .FirstOrDefaultAsync();
                if ((currentEventId != 0 && !await OwnsEventAsync(currentEventId)) ||
                    !await OwnsEventAsync(category.EventId))
                {
                    return Forbid();
                }
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
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

        // POST: api/Categories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            // POST always creates a new row. The id is never taken from the client.
            category.CategoryId = 0;

            if (HasSignedInCaller && !await OwnsEventAsync(category.EventId))
            {
                return Forbid();
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCategory", new { id = category.CategoryId }, category);
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (HasSignedInCaller && !await OwnsEventAsync(category.EventId))
            {
                return Forbid();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // [Authorize] already rejects anonymous callers, so this is only false when the controller is
        // exercised directly in unit tests without an HTTP context.
        private bool HasSignedInCaller => HttpContext?.User?.Identity?.IsAuthenticated == true;

        private async Task<bool> OwnsEventAsync(int eventId)
        {
            var userId = HttpContext.User.GetUserId();
            return await _context.Events.AnyAsync(e => e.EventId == eventId && e.OrganiserId == userId);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
