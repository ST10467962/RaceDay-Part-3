using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Models.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    /// <summary>The logged-in user's own profile. The user is always taken from the auth cookie.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IBlobStorageService _blobs;

        public ProfileController(RaceDayDbContext context, IBlobStorageService blobs)
        {
            _context = context;
            _blobs = blobs;
        }

        // GET: api/Profile
        [HttpGet]
        public async Task<ActionResult<ProfileDto>> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            return await BuildProfileAsync(user);
        }

        // PUT: api/Profile
        [HttpPut]
        public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value.Date >= DateTime.Today)
                return BadRequest(new { message = "Date of birth must be in the past." });

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

            if (user.Role == "Participant")
            {
                var participant = await ParticipantProvisioner.EnsureParticipantAsync(_context, user);
                participant.FirstName = user.FirstName;
                participant.LastName = user.LastName;
                participant.PhoneNumber = user.PhoneNumber;
                participant.Gender = request.Gender?.Trim() ?? string.Empty;
                participant.Address = request.Address?.Trim() ?? string.Empty;
                participant.EmergencyContact = request.EmergencyContact?.Trim() ?? string.Empty;
                if (request.DateOfBirth.HasValue)
                    participant.DateOfBirth = request.DateOfBirth.Value.Date;
            }

            await _context.SaveChangesAsync();
            return await BuildProfileAsync(user);
        }

        // POST: api/Profile/picture   (multipart/form-data, field name "file")
        [HttpPost("picture")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadPicture([FromForm] IFormFile file, CancellationToken ct)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var problem = BlobStorageService.Validate(file);
            if (problem != null) return BadRequest(new { message = problem });

            var oldName = user.ProfilePictureName;
            user.ProfilePictureName = await _blobs.UploadImageAsync(file, "profiles", ct);
            await _context.SaveChangesAsync(ct);
            await _blobs.DeleteAsync(oldName, ct);

            return Ok(new { profilePictureName = user.ProfilePictureName });
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userId = User.GetUserId();
            return userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
        }

        private async Task<ProfileDto> BuildProfileAsync(User user)
        {
            var dto = new ProfileDto
            {
                UserId = user.UserId,
                Role = user.Role,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureName = user.ProfilePictureName
            };

            if (user.Role == "Participant")
            {
                var participant = await ParticipantProvisioner.EnsureParticipantAsync(_context, user);
                dto.ParticipantId = participant.ParticipantId;
                dto.DateOfBirth = participant.DateOfBirth;
                dto.Gender = participant.Gender;
                dto.Address = participant.Address;
                dto.EmergencyContact = participant.EmergencyContact;
            }

            return dto;
        }
    }
}
