using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<RaceRoute> RaceRoutes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {
            // ✅ Configure decimal precision
            modelBuilder.Entity<RaceRoute>()
                .Property(r => r.Distance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Category>()
                .Property(c => c.Distance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Category>()
                .Property(c => c.EntryFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Event>()
                .Property(e => e.Distance)
                .HasPrecision(18, 2);

            // One participant profile per login account
            modelBuilder.Entity<Participant>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // Avoid multiple cascade paths on Results (SQL Server restriction)
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Event)
                .WithMany()
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Participant)
                .WithMany(p => p.Results)
                .HasForeignKey(r => r.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
