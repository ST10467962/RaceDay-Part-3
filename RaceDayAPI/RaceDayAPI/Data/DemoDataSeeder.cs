using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Data
{
    /// <summary>
    /// Creates a realistic demo dataset so the app is usable straight after the database is created.
    ///
    /// The seeder is idempotent: every row is looked up by a natural key (email, event name,
    /// category name, and so on) and only added when it is missing. Running it twice, or running
    /// it against a database that already has some of the demo rows, is safe.
    ///
    /// Every demo account uses the password <see cref="DemoPassword"/>.
    /// </summary>
    public static class DemoDataSeeder
    {
        public const string DemoPassword = "password321";

        // All demo e-mail addresses are fixed so the seeder can recognise its own rows.
        public const string Organiser1Email = "thabo.mokoena@raceday.co.za";
        public const string Organiser2Email = "lerato.dlamini@raceday.co.za";
        public const string Participant1Email = "sipho.ndlovu@example.com";
        public const string Participant2Email = "aisha.patel@example.com";
        public const string Participant3Email = "johan.vdmerwe@example.com";
        public const string Participant4Email = "nomsa.khumalo@example.com";

        public static async Task SeedAsync(
            RaceDayDbContext db,
            IAuthService auth,
            ILogger logger,
            CancellationToken ct = default)
        {
            var today = DateTime.Today;

            // ---------- Organisers ----------
            var thabo = await EnsureOrganiserAsync(db, auth,
                Organiser1Email, "Thabo", "Mokoena", "082 555 0101", ct);

            var lerato = await EnsureOrganiserAsync(db, auth,
                Organiser2Email, "Lerato", "Dlamini", "083 555 0102", ct);

            // ---------- Participants ----------
            var sipho = await EnsureParticipantAsync(db, auth,
                Participant1Email, "Sipho", "Ndlovu", "071 555 0201",
                new DateTime(1990, 3, 14), "Male",
                "12 Vilakazi Street, Soweto, Johannesburg", "071 555 9201", ct);

            var aisha = await EnsureParticipantAsync(db, auth,
                Participant2Email, "Aisha", "Patel", "072 555 0202",
                new DateTime(1985, 7, 22), "Female",
                "45 Florida Road, Durban", "072 555 9202", ct);

            var johan = await EnsureParticipantAsync(db, auth,
                Participant3Email, "Johan", "van der Merwe", "073 555 0203",
                new DateTime(1978, 11, 2), "Male",
                "8 Stellenbosch Avenue, Cape Town", "073 555 9203", ct);

            var nomsa = await EnsureParticipantAsync(db, auth,
                Participant4Email, "Nomsa", "Khumalo", "074 555 0204",
                new DateTime(1995, 5, 30), "Female",
                "22 Main Road, Pretoria", "074 555 9204", ct);

            // ---------- Events ----------
            // Two past events (results can be captured) and two upcoming events (enrolments are open).
            var sunsetHalf = await EnsureEventAsync(db, thabo,
                "Cape Town Sunset Half Marathon",
                today.AddDays(-21),
                "Sea Point Promenade, Cape Town",
                "A flat, fast half marathon along the Atlantic seaboard at golden hour. " +
                "Water points every 3 km and a medal for every finisher.",
                new TimeSpan(17, 0, 0), new TimeSpan(21, 0, 0),
                21.1m, "Road Run", today.AddDays(-60), ct);

            var trailChallenge = await EnsureEventAsync(db, thabo,
                "Joburg Trail Challenge",
                today.AddDays(30),
                "Delta Park, Johannesburg",
                "A technical 15 km trail run through Delta Park's single-track and river paths. " +
                "Expect mud, jacarandas and a proper Joburg sunrise.",
                new TimeSpan(6, 30, 0), new TimeSpan(10, 0, 0),
                15m, "Trail Run", today.AddDays(-10), ct);

            var beachfrontFunRun = await EnsureEventAsync(db, lerato,
                "Durban Beachfront Fun Run",
                today.AddDays(-60),
                "Durban Beachfront, Durban",
                "A family-friendly 5 km along the Golden Mile. Walkers, prams and dogs on leads are welcome.",
                new TimeSpan(7, 0, 0), new TimeSpan(9, 0, 0),
                5m, "Fun Run", today.AddDays(-90), ct);

            var springCycle = await EnsureEventAsync(db, lerato,
                "Pretoria Spring Cycle",
                today.AddDays(45),
                "Union Buildings, Pretoria",
                "A 40 km road cycle from the Union Buildings out towards Hartbeespoort. " +
                "Rolling hills, a neutral zone through the city and a water point at the halfway mark.",
                new TimeSpan(6, 0, 0), new TimeSpan(11, 0, 0),
                40m, "Cycling", today.AddDays(-5), ct);

            // ---------- Categories ----------
            var sunsetOpen = await EnsureCategoryAsync(db,
                sunsetHalf, "Open 18-39", 21.1m, 450m, "18-39", ct);
            var sunsetVeterans = await EnsureCategoryAsync(db,
                sunsetHalf, "Veterans 40-49", 21.1m, 400m, "40-49", ct);
            var sunsetMasters = await EnsureCategoryAsync(db,
                sunsetHalf, "Masters 50+", 21.1m, 350m, "50+", ct);

            var trailOpen = await EnsureCategoryAsync(db,
                trailChallenge, "Trail Open", 15m, 300m, "18-39", ct);
            var trailVeterans = await EnsureCategoryAsync(db,
                trailChallenge, "Trail Veterans", 15m, 250m, "40+", ct);

            var funRunOpen = await EnsureCategoryAsync(db,
                beachfrontFunRun, "Fun Run Open", 5m, 100m, "All ages", ct);
            var funRunJunior = await EnsureCategoryAsync(db,
                beachfrontFunRun, "Junior 5 km", 5m, 50m, "Under 18", ct);

            var cycleOpen = await EnsureCategoryAsync(db,
                springCycle, "Road Bike Open", 40m, 350m, "18-39", ct);
            var cycleMasters = await EnsureCategoryAsync(db,
                springCycle, "Road Bike Masters", 40m, 300m, "40+", ct);

            // ---------- Routes ----------
            await EnsureRouteAsync(db, sunsetHalf,
                "Sea Point Coastal Loop", 21.1m,
                "Sea Point Promenade (Beach Road)", "Green Point Lighthouse", "Road", ct);
            await EnsureRouteAsync(db, sunsetHalf,
                "Sunset 5 km Fun Route", 5m,
                "Sea Point Promenade", "Mouille Point", "Road", ct);
            await EnsureRouteAsync(db, trailChallenge,
                "Delta Park Trail Loop", 15m,
                "Delta Park Main Gate", "Delta Park Main Gate", "Trail", ct);
            await EnsureRouteAsync(db, beachfrontFunRun,
                "Golden Mile Promenade", 5m,
                "uShaka Marine World", "Blue Lagoon", "Road", ct);
            await EnsureRouteAsync(db, springCycle,
                "Union Buildings to Hartbeespoort", 40m,
                "Union Buildings, Pretoria", "Hartbeespoort Dam", "Road", ct);
            await EnsureRouteAsync(db, springCycle,
                "Spring Cycle 10 km Family Route", 10m,
                "Union Buildings, Pretoria", "Bryntirion Estate", "Road", ct);

            // ---------- Enrolments (entries) ----------
            // Historical entries that already have results.
            var siphoHalf = await EnsureEntryAsync(db, sipho, sunsetOpen, "Confirmed", true, today.AddDays(-30), ct);
            var aishaHalf = await EnsureEntryAsync(db, aisha, sunsetOpen, "Confirmed", true, today.AddDays(-29), ct);
            var johanHalf = await EnsureEntryAsync(db, johan, sunsetVeterans, "Confirmed", true, today.AddDays(-28), ct);
            var nomsaHalf = await EnsureEntryAsync(db, nomsa, sunsetOpen, "Confirmed", true, today.AddDays(-27), ct);

            var johanFunRun = await EnsureEntryAsync(db, johan, funRunOpen, "Confirmed", true, today.AddDays(-75), ct);
            var nomsaFunRun = await EnsureEntryAsync(db, nomsa, funRunOpen, "Confirmed", true, today.AddDays(-74), ct);

            // Three active enrolments in upcoming events: two confirmed/pending trail entries
            // and one pending cycle entry.
            await EnsureEntryAsync(db, sipho, trailOpen, "Confirmed", true, today.AddDays(-2), ct);
            await EnsureEntryAsync(db, aisha, trailOpen, "Pending", false, today.AddDays(-1), ct);
            await EnsureEntryAsync(db, nomsa, cycleOpen, "Pending", false, today, ct);

            // ---------- Results ----------
            // Position must follow finish time, so the times below are in ascending order.
            await EnsureResultAsync(db, siphoHalf, sunsetHalf.EventId, new TimeSpan(1, 18, 42), 1, ct);
            await EnsureResultAsync(db, aishaHalf, sunsetHalf.EventId, new TimeSpan(1, 24, 15), 2, ct);
            await EnsureResultAsync(db, nomsaHalf, sunsetHalf.EventId, new TimeSpan(1, 31, 8), 3, ct);
            await EnsureResultAsync(db, johanHalf, sunsetHalf.EventId, new TimeSpan(1, 42, 55), 4, ct);

            await EnsureResultAsync(db, nomsaFunRun, beachfrontFunRun.EventId, new TimeSpan(0, 28, 44), 1, ct);
            await EnsureResultAsync(db, johanFunRun, beachfrontFunRun.EventId, new TimeSpan(0, 31, 12), 2, ct);

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "RaceDay demo data is ready. Sign in with any demo account using the password \"{Password}\".",
                DemoPassword);
        }

        // ------------------------------------------------------------------ helpers

        private static async Task<User> EnsureOrganiserAsync(
            RaceDayDbContext db, IAuthService auth,
            string email, string firstName, string lastName, string phone,
            CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user != null) return user;

            user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = auth.HashPassword(DemoPassword),
                PhoneNumber = phone,
                Role = "Organiser",
                CreatedAt = DateTime.UtcNow.AddDays(-120)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return user;
        }

        private static async Task<Participant> EnsureParticipantAsync(
            RaceDayDbContext db, IAuthService auth,
            string email, string firstName, string lastName, string phone,
            DateTime dateOfBirth, string gender, string address, string emergencyContact,
            CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user == null)
            {
                user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = auth.HashPassword(DemoPassword),
                    PhoneNumber = phone,
                    Role = "Participant",
                    CreatedAt = DateTime.UtcNow.AddDays(-100)
                };

                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }

            var participant = await db.Participants.FirstOrDefaultAsync(p => p.UserId == user.UserId, ct);
            if (participant != null) return participant;

            participant = new Participant
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                PhoneNumber = user.PhoneNumber,
                Address = address,
                EmergencyContact = emergencyContact
            };

            db.Participants.Add(participant);
            await db.SaveChangesAsync(ct);
            return participant;
        }

        private static async Task<Event> EnsureEventAsync(
            RaceDayDbContext db, User organiser,
            string name, DateTime date, string location, string description,
            TimeSpan? startTime, TimeSpan? endTime,
            decimal distance, string eventType, DateTime createdAt,
            CancellationToken ct)
        {
            var @event = await db.Events.FirstOrDefaultAsync(
                e => e.EventName == name && e.OrganiserId == organiser.UserId, ct);

            if (@event != null) return @event;

            @event = new Event
            {
                OrganiserId = organiser.UserId,
                EventName = name,
                EventDate = date.Date,
                Location = location,
                Description = description,
                StartTime = startTime,
                EndTime = endTime,
                Distance = distance,
                EventType = eventType,
                CreatedAt = createdAt
            };

            db.Events.Add(@event);
            await db.SaveChangesAsync(ct);
            return @event;
        }

        private static async Task<Category> EnsureCategoryAsync(
            RaceDayDbContext db, Event @event,
            string name, decimal distance, decimal entryFee, string ageGroup,
            CancellationToken ct)
        {
            var category = await db.Categories.FirstOrDefaultAsync(
                c => c.EventId == @event.EventId && c.CategoryName == name, ct);

            if (category != null) return category;

            category = new Category
            {
                EventId = @event.EventId,
                CategoryName = name,
                Distance = distance,
                EntryFee = entryFee,
                AgeGroup = ageGroup
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);
            return category;
        }

        private static async Task EnsureRouteAsync(
            RaceDayDbContext db, Event @event,
            string name, decimal distance, string startPoint, string endPoint, string terrain,
            CancellationToken ct)
        {
            var exists = await db.RaceRoutes.AnyAsync(
                r => r.EventId == @event.EventId && r.RouteName == name, ct);
            if (exists) return;

            db.RaceRoutes.Add(new RaceRoute
            {
                EventId = @event.EventId,
                RouteName = name,
                Distance = distance,
                StartPoint = startPoint,
                EndPoint = endPoint,
                TerrainType = terrain
            });

            await db.SaveChangesAsync(ct);
        }

        private static async Task<Entry> EnsureEntryAsync(
            RaceDayDbContext db, Participant participant, Category category,
            string status, bool paid, DateTime entryDate,
            CancellationToken ct)
        {
            // One active enrolment per participant per event, whatever the category.
            var entry = await db.Entries.FirstOrDefaultAsync(
                e => e.ParticipantId == participant.ParticipantId && e.Category.EventId == category.EventId, ct);

            if (entry != null) return entry;

            entry = new Entry
            {
                ParticipantId = participant.ParticipantId,
                CategoryId = category.CategoryId,
                EntryDate = entryDate,
                PaymentStatus = paid,
                Status = status
            };

            db.Entries.Add(entry);
            await db.SaveChangesAsync(ct);
            return entry;
        }

        private static async Task EnsureResultAsync(
            RaceDayDbContext db, Entry entry, int eventId, TimeSpan finishTime, int position,
            CancellationToken ct)
        {
            var exists = await db.Results.AnyAsync(r => r.EntryId == entry.EntryId, ct);
            if (exists) return;

            db.Results.Add(new Result
            {
                EntryId = entry.EntryId,
                EventId = eventId,
                ParticipantId = entry.ParticipantId,
                FinishTime = finishTime,
                Position = position,
                RecordedAt = DateTime.UtcNow.AddDays(-7)
            });

            await db.SaveChangesAsync(ct);
        }
    }
}
