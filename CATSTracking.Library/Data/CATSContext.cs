using CATSTracking.Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CATSTracking.Library.Data

{
    public class CATSContext : IdentityDbContext<IdentityUser, IdentityRole, string,
        IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>,
        IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public CATSContext(DbContextOptions<CATSContext> options) : base(options) { }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Prevent EF from creating cascading deletes
            // to the tracker table. I.e. removing a user
            // or a company does not mean we remove the tracker as well

            builder.Entity<TrackerActivity>()
                .HasOne(a => a.TrackerEntity)
                .WithMany()
                .HasForeignKey(a => a.TrackerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CompanyTracker>()
                .HasOne(ct => ct.TrackerEntity)
                .WithMany()
                .HasForeignKey(ct => ct.TrackerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserTracker>()
                .HasOne(ut => ut.TrackerObj)
                .WithMany()
                .HasForeignKey(ut => ut.TrackerId)
                .OnDelete(DeleteBehavior.Restrict);


            // When we delete a notificationuser
            // link, we can delete the notification to
            // avoid clutter and holding onto stale
            // notification data. However, this should
            // NOT delete a user, so we restrict the
            // cascading deletes.
            builder.Entity<NotificationUser>()
                .HasOne(x => x.NotificationEntity)
                .WithMany()
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            
            builder.Entity<NotificationUser>()
                .HasOne(x => x.Login)
                .WithMany()
                .HasForeignKey(x => x.LoginId)
                .OnDelete(DeleteBehavior.Restrict);
        }


        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }

        public DbSet<Tracker> Trackers { get; set; }
        public DbSet<TrackerActivity> TrackerActivities { get; set; }
        public DbSet<CompanyTracker> CompanyTrackers { get; set; }
        public DbSet<UserTracker> UserTrackers { get; set; }
        
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationCompany> NotificationCompanies { get; set; }
        public DbSet<NotificationUser> NotificationUsers { get; set; }

        public DbSet<PlatformSetting> PlatformSettings { get; set; }
        public DbSet<EventLog> EventLogs { get; set; }
        public DbSet<Preference> Preferences { get; set; }
        public DbSet<SMS> SMSes { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }


    }
}
