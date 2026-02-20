using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RainfallThree.Models;

namespace RainfallThree.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RainfallSheet> RainfallSheets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RainfallSheet>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("rainfall_sheet");

                entity.Property(e => e.Index).HasColumnName("index");
                entity.Property(e => e.Latdeg).HasColumnName("LATDEG");
                entity.Property(e => e.Latmin).HasColumnName("LATMIN");
                entity.Property(e => e.Longdeg).HasColumnName("LONGDEG");
                entity.Property(e => e.Longmin).HasColumnName("LONGMIN");
                entity.Property(e => e.SourceSheet).HasColumnName("Source_Sheet");
                entity.Property(e => e._10080Min).HasColumnName("_10080_min");
                entity.Property(e => e._10Min).HasColumnName("_10_min");
                entity.Property(e => e._120Min).HasColumnName("_120_min");
                entity.Property(e => e._1440Min).HasColumnName("_1440_min");
                entity.Property(e => e._15Min).HasColumnName("_15_min");
                entity.Property(e => e._30Min).HasColumnName("_30_min");
                entity.Property(e => e._4320Min).HasColumnName("_4320_min");
                entity.Property(e => e._5Min).HasColumnName("_5_min");
                entity.Property(e => e._60Min).HasColumnName("_60_min");
            });
        }
    }
}
