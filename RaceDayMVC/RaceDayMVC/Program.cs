using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Stops non-nullable strings on view models being treated as [Required] automatically.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

    // Every POST needs a valid anti-forgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Turns API failures into friendly pages instead of error dumps.
    options.Filters.Add<ApiExceptionFilter>();
});

builder.Services.AddAntiforgery(options => options.Cookie.Name = ".RaceDay.Antiforgery");

// ---- Session: this is how the MVC app remembers who is logged in and their role ----
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.Name = ".RaceDay.MvcSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();

// ---- The Part 2 API: the only place this app gets or saves data ----
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiSettings:BaseUrl is not configured. Set it to the address of the RaceDay API.");
}
var timeoutSeconds = builder.Configuration.GetValue("ApiSettings:TimeoutSeconds", 30);

builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    })
    // Cookies are handled per visitor inside ApiClient; the shared handler must not keep any.
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false });

var app = builder.Build();

// Never show stack traces or raw error text to users, in any environment.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();

// One culture everywhere so "5.5" in a number box always means five and a half,
// whatever the server's regional settings are.
var cultures = new[] { CultureInfo.InvariantCulture };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = cultures,
    SupportedUICultures = cultures
});

app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
