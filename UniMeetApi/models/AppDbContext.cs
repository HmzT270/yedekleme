using Microsoft.EntityFrameworkCore;

namespace UniMeetApi
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users  => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Club> Clubs => Set<Club>();

        //  YENİ:
        public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
        public DbSet<FavoriteEvent> FavoriteEvents => Set<FavoriteEvent>();
        public DbSet<EventAttendee> EventAttendees => Set<EventAttendee>();
        public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ClubMembers konfigürasyonu
            modelBuilder.Entity<ClubMember>(e =>
            {
                e.HasKey(x => new { x.UserId, x.ClubId }); // composite PK
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.ClubId);

                e.HasOne(x => x.User)
                 .WithMany() // istersen User içine ICollection<ClubMember> Members ekleyip .WithMany(u => u.Members) yapabilirsin
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Club)
                 .WithMany() // istersen Club içine ICollection<ClubMember> Members ekleyip .WithMany(c => c.Members) yapabilirsin
                 .HasForeignKey(x => x.ClubId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // FavoriteEvents (User <-> Event many-to-many via explicit join)
            modelBuilder.Entity<FavoriteEvent>(e =>
            {
                e.HasKey(x => new { x.UserId, x.EventId });
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.EventId);

                e.HasOne(x => x.User)
                 .WithMany() // keep User lightweight; navigation is optional
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Event)
                 .WithMany() // keep Event lightweight; navigation is optional
                 .HasForeignKey(x => x.EventId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // EventAttendees (registrations)
            modelBuilder.Entity<EventAttendee>(e =>
            {
                e.HasKey(x => new { x.UserId, x.EventId });
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.EventId);

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Event)
                 .WithMany()
                 .HasForeignKey(x => x.EventId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
