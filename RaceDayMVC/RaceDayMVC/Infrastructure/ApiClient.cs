using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDayMVC.Models.Api;

namespace RaceDayMVC.Infrastructure
{
    /// <summary>
    /// The only class in the MVC project that talks to the outside world. Every call goes to the Part 2 API.
    ///
    /// The API logs people in with a cookie. A shared HttpClient must never hold that cookie (it would be
    /// shared by every visitor), so after login the API's cookie is kept in this visitor's MVC session
    /// and replayed on each request.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
        {
            _http = http;
            _accessor = accessor;
            _logger = logger;
        }

        private ISession Session =>
            _accessor.HttpContext?.Session ?? throw new InvalidOperationException("No active HTTP context.");

        // ------------------------------------------------------------------ auth

        public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
            PostAsync<AuthResponse>("api/Auth/login", request, ct);

        public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
            PostAsync<AuthResponse>("api/Auth/register", request, ct);

        /// <summary>Ends the API's login too. Failures are ignored because the MVC session is cleared anyway.</summary>
        public async Task LogoutAsync(CancellationToken ct = default)
        {
            try
            {
                using var response = await SendAsync(HttpMethod.Post, "api/Auth/logout", null, ct);
            }
            catch (ApiException ex)
            {
                _logger.LogInformation("API logout skipped: {Message}", ex.Message);
            }
        }

        // ------------------------------------------------------------------ events

        public Task<List<EventDto>> GetEventsAsync(EventQuery? query = null, CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, string?>();
            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search)) parameters["search"] = query.Search.Trim();
                if (!string.IsNullOrWhiteSpace(query.EventType)) parameters["eventType"] = query.EventType;
                if (query.MinDistance.HasValue) parameters["minDistance"] = Invariant(query.MinDistance.Value);
                if (query.MaxDistance.HasValue) parameters["maxDistance"] = Invariant(query.MaxDistance.Value);
                if (query.DateFrom.HasValue) parameters["dateFrom"] = query.DateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (query.DateTo.HasValue) parameters["dateTo"] = query.DateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (query.Upcoming) parameters["upcoming"] = "true";
                if (query.Mine) parameters["mine"] = "true";
            }

            return GetAsync<List<EventDto>>(QueryHelpers.AddQueryString("api/Events", parameters), ct);
        }

        public Task<EventDto> GetEventAsync(int id, CancellationToken ct = default) =>
            GetAsync<EventDto>($"api/Events/{id}", ct);

        public Task<EventDto> CreateEventAsync(EventRequest request, CancellationToken ct = default) =>
            PostAsync<EventDto>("api/Events", request, ct);

        public Task UpdateEventAsync(int id, EventRequest request, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Put, $"api/Events/{id}", JsonContent.Create(request), ct);

        public Task DeleteEventAsync(int id, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Delete, $"api/Events/{id}", null, ct);

        public Task UploadBannerAsync(int eventId, IFormFile file, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Post, $"api/Events/{eventId}/banner", BuildFileContent(file), ct);

        // ------------------------------------------------------------------ categories

        public Task<List<CategoryDto>> GetCategoriesAsync(int eventId, CancellationToken ct = default) =>
            GetAsync<List<CategoryDto>>($"api/Categories?eventId={eventId}", ct);

        public Task<CategoryDto> GetCategoryAsync(int id, CancellationToken ct = default) =>
            GetAsync<CategoryDto>($"api/Categories/{id}", ct);

        public Task<CategoryDto> CreateCategoryAsync(CategoryDto category, CancellationToken ct = default) =>
            PostAsync<CategoryDto>("api/Categories", category, ct);

        public Task UpdateCategoryAsync(CategoryDto category, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Put, $"api/Categories/{category.CategoryId}", JsonContent.Create(category), ct);

        public Task DeleteCategoryAsync(int id, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Delete, $"api/Categories/{id}", null, ct);

        // ------------------------------------------------------------------ enrolments

        public Task<List<EntryDto>> GetEntriesAsync(int? eventId = null, CancellationToken ct = default)
        {
            var url = eventId.HasValue ? $"api/Entries?eventId={eventId.Value}" : "api/Entries";
            return GetAsync<List<EntryDto>>(url, ct);
        }

        public Task<EntryDto> EnrolAsync(int categoryId, CancellationToken ct = default) =>
            PostAsync<EntryDto>("api/Entries", new EnrolRequest { CategoryId = categoryId }, ct);

        public Task UpdateEntryStatusAsync(int entryId, string status, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Put, $"api/Entries/{entryId}/status",
                JsonContent.Create(new EntryStatusRequest { Status = status }), ct);

        // ------------------------------------------------------------------ results

        public Task<List<ResultDto>> GetResultsAsync(int? eventId = null, CancellationToken ct = default)
        {
            var url = eventId.HasValue ? $"api/Results?eventId={eventId.Value}" : "api/Results";
            return GetAsync<List<ResultDto>>(url, ct);
        }

        public Task SaveResultsAsync(int eventId, List<ResultRequest> results, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Post, $"api/Results/event/{eventId}", JsonContent.Create(results), ct);

        // ------------------------------------------------------------------ profile

        public Task<ProfileDto> GetProfileAsync(CancellationToken ct = default) =>
            GetAsync<ProfileDto>("api/Profile", ct);

        public Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
            SendAsync<ProfileDto>(HttpMethod.Put, "api/Profile", JsonContent.Create(request), ct);

        public Task UploadProfilePictureAsync(IFormFile file, CancellationToken ct = default) =>
            SendNoContentAsync(HttpMethod.Post, "api/Profile/picture", BuildFileContent(file), ct);

        // ------------------------------------------------------------------ files

        /// <summary>Fetches an image through the API. Returns null if it doesn't exist.</summary>
        public async Task<(byte[] Bytes, string ContentType)?> GetFileAsync(string blobName, CancellationToken ct = default)
        {
            try
            {
                using var response = await SendAsync(HttpMethod.Get, $"api/Files/{blobName}", null, ct);
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                return (bytes, contentType);
            }
            catch (ApiNotFoundException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ plumbing

        private Task<T> GetAsync<T>(string url, CancellationToken ct) =>
            SendAsync<T>(HttpMethod.Get, url, null, ct);

        private Task<T> PostAsync<T>(string url, object body, CancellationToken ct) =>
            SendAsync<T>(HttpMethod.Post, url, JsonContent.Create(body, body.GetType()), ct);

        private async Task<T> SendAsync<T>(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
        {
            using var response = await SendAsync(method, url, content, ct);

            try
            {
                var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                if (value == null)
                    throw new ApiException(HttpStatusCode.BadGateway, "The data service sent back an empty response.");
                return value;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Could not read the API response from {Url}", url);
                throw new ApiException(HttpStatusCode.BadGateway, "The data service sent back something RaceDay couldn't read.");
            }
        }

        private async Task SendNoContentAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
        {
            using var response = await SendAsync(method, url, content, ct);
        }

        /// <summary>Sends one request with this visitor's API cookie and turns failures into ApiExceptions.</summary>
        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var cookies = Session.GetString(SessionKeys.ApiCookies);
            if (!string.IsNullOrEmpty(cookies))
                request.Headers.TryAddWithoutValidation("Cookie", cookies);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API unreachable: {Method} {Url}", method, url);
                throw new ApiUnavailableException();
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "API timed out: {Method} {Url}", method, url);
                throw new ApiUnavailableException();
            }

            StoreCookies(response);

            if (response.IsSuccessStatusCode)
                return response;

            try
            {
                throw await BuildExceptionAsync(response, ct);
            }
            finally
            {
                response.Dispose();
            }
        }

        private static MultipartFormDataContent BuildFileContent(IFormFile file)
        {
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

            var form = new MultipartFormDataContent();
            form.Add(fileContent, "file", file.FileName);
            return form;
        }

        // ---- cookie jar (per visitor, kept in the MVC session) ----

        private void StoreCookies(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies)) return;

            var jar = LoadCookies();
            foreach (var header in setCookies)
            {
                var pair = header.Split(';')[0];
                var equals = pair.IndexOf('=');
                if (equals <= 0) continue;

                var name = pair[..equals].Trim();
                var value = pair[(equals + 1)..].Trim();

                var expired = header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrEmpty(value) || expired)
                    jar.Remove(name);
                else
                    jar[name] = value;
            }

            Session.SetString(SessionKeys.ApiCookies,
                string.Join("; ", jar.Select(c => $"{c.Key}={c.Value}")));
        }

        private Dictionary<string, string> LoadCookies()
        {
            var jar = new Dictionary<string, string>();
            var stored = Session.GetString(SessionKeys.ApiCookies);
            if (string.IsNullOrEmpty(stored)) return jar;

            foreach (var part in stored.Split("; ", StringSplitOptions.RemoveEmptyEntries))
            {
                var equals = part.IndexOf('=');
                if (equals > 0) jar[part[..equals]] = part[(equals + 1)..];
            }
            return jar;
        }

        // ---- turning API error bodies into readable messages ----

        private static async Task<ApiException> BuildExceptionAsync(HttpResponseMessage response, CancellationToken ct)
        {
            var message = await ExtractMessageAsync(response, ct);

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ApiUnauthorizedException(message ?? "Your session has ended. Please log in again."),
                HttpStatusCode.Forbidden => new ApiForbiddenException(message ?? "You don't have permission to do that."),
                HttpStatusCode.NotFound => new ApiNotFoundException(message ?? "That item could not be found."),
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity =>
                    new ApiException(response.StatusCode, message ?? "Please check the details you entered and try again."),
                _ => new ApiException(response.StatusCode, "Something went wrong on our side. Please try again.")
            };
        }

        /// <summary>Reads { "message": "..." } or ASP.NET validation errors ({ "errors": { "Field": ["..."] } }).</summary>
        private static async Task<string?> ExtractMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(body)) return null;

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                if (root.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(message.GetString()))
                {
                    return message.GetString();
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var builder = new StringBuilder();
                    foreach (var field in errors.EnumerateObject())
                    {
                        if (field.Value.ValueKind != JsonValueKind.Array) continue;
                        foreach (var item in field.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                                builder.Append(item.GetString()).Append(' ');
                        }
                    }
                    var text = builder.ToString().Trim();
                    if (text.Length > 0) return text;
                }
            }
            catch (JsonException)
            {
                // Not JSON; fall through to the generic message.
            }

            return null;
        }

        private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
