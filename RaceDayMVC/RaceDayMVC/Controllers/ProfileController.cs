using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize(Role = "Participant")]
    public class ProfileController : Controller
    {
        private readonly ApiClient _api;

        public ProfileController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var profile = await _api.GetProfileAsync();

            return View(new ProfileViewModel
            {
                Email = profile.Email,
                ProfilePictureName = profile.ProfilePictureName,
                Initials = (profile.FirstName[..Math.Min(1, profile.FirstName.Length)] +
                            profile.LastName[..Math.Min(1, profile.LastName.Length)]).ToUpperInvariant(),
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhoneNumber = profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                Address = profile.Address,
                EmergencyContact = profile.EmergencyContact
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            var user = HttpContext.Session.GetUser()!;

            var pictureProblem = ImageRules.Validate(model.Picture);
            if (pictureProblem != null)
                ModelState.AddModelError(nameof(model.Picture), pictureProblem);

            if (!ModelState.IsValid)
            {
                FillReadOnlyFields(model, user);
                return View("Index", model);
            }

            try
            {
                // Picture first, so the profile response that follows already includes it.
                if (ImageRules.HasFile(model.Picture))
                    await _api.UploadProfilePictureAsync(model.Picture!);

                var updated = await _api.UpdateProfileAsync(new UpdateProfileRequest
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender ?? string.Empty,
                    Address = model.Address?.Trim() ?? string.Empty,
                    EmergencyContact = model.EmergencyContact?.Trim() ?? string.Empty
                });

                // Keep the name and picture in the navigation bar up to date.
                user.FirstName = updated.FirstName;
                user.LastName = updated.LastName;
                user.ProfilePictureName = updated.ProfilePictureName;
                HttpContext.Session.SetUser(user);

                TempData["Success"] = "Your profile has been updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                FillReadOnlyFields(model, user);
                return View("Index", model);
            }
        }

        private static void FillReadOnlyFields(ProfileViewModel model, SessionUser user)
        {
            model.Email = user.Email;
            model.ProfilePictureName = user.ProfilePictureName;
            model.Initials = user.Initials;
        }
    }
}
