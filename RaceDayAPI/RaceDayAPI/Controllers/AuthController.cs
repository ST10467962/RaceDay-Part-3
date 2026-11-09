using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(RaceDayDbContext context, IAuthService authService, IConfiguration configuration)
        {
            _context = context;
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Success = false, Message = "Email and password are required." });

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest(new AuthResponse { Success = false, Message = "First name and last name are required." });

            if (request.Password.Length < 8)
                return BadRequest(new AuthResponse { Success = false, Message = "Your password needs at least 8 characters." });

            if (request.Role != "Organiser" && request.Role != "Participant")
                return BadRequest(new AuthResponse { Success = false, Message = "Role must be 'Organiser' or 'Participant'." });

            if (request.Role == "Organiser" && !OrganiserCodeIsValid(request.OrganiserAccessCode))
                return BadRequest(new AuthResponse { Success = false, Message = "The organiser access code is not valid." });

            if (request.Role == "Participant")
            {
                if (!request.DateOfBirth.HasValue)
                    return BadRequest(new AuthResponse { Success = false, Message = "Enter your date of birth." });
                if (request.DateOfBirth.Value.Date >= DateTime.Today)
                    return BadRequest(new AuthResponse { Success = false, Message = "Date of birth must be in the past." });
            }

            var email = request.Email.Trim();
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
                return BadRequest(new AuthResponse { Success = false, Message = "User with this email already exists." });

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = _authService.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Participants get a profile row linked to their login so they can enrol in events.
            if (user.Role == "Participant")
            {
                await ParticipantProvisioner.EnsureParticipantAsync(_context, user, request.DateOfBirth, request.Gender);
            }

            await SignInUserAsync(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Registration successful.",
                User = await BuildUserDtoAsync(user)
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Success = false, Message = "Email and password are required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim());
            if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid email or password." });

            // Upgrade accounts still holding a Part 2 hash to the current format.
            if (_authService.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = _authService.HashPassword(request.Password);
                await _context.SaveChangesAsync();
            }

            await SignInUserAsync(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Login successful.",
                User = await BuildUserDtoAsync(user)
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return Ok(new { Success = true, Message = "Logged out successfully." });
        }

        /// <summary>
        /// Organiser accounts need the access code configured by the club. If no code is configured,
        /// organiser registration is closed. Compared in constant time.
        /// </summary>
        private bool OrganiserCodeIsValid(string? supplied)
        {
            var expected = _configuration["OrganiserRegistration:AccessCode"];
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied)) return false;

            var expectedBytes = Encoding.UTF8.GetBytes(expected.Trim());
            var suppliedBytes = Encoding.UTF8.GetBytes(supplied.Trim());
            return expectedBytes.Length == suppliedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
        }

        private async Task<UserDto> BuildUserDtoAsync(User user)
        {
            int? participantId = null;
            if (user.Role == "Participant")
            {
                // Also repairs accounts created in Part 2, which have no participant profile yet.
                var participant = await ParticipantProvisioner.EnsureParticipantAsync(_context, user);
                participantId = participant.ParticipantId;
            }

            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                ParticipantId = participantId,
                ProfilePictureName = user.ProfilePictureName
            };
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                });

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("Email", user.Email);
        }
    }
}
