using Microsoft.EntityFrameworkCore;
using OdectyStat1.Business;

namespace OdectyStat1.DataLayer
{
    public class HomeAssistantDbContext : DbContext
    {
        public HomeAssistantDbContext(DbContextOptions<HomeAssistantDbContext> options) : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }

        public DbSet<Statistic> Statistics { get; set; }

        public DbSet<StatisticsShortTerm> StatisticsShortTerms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Statistic>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.ToTable("statistics");

                entity.HasIndex(e => e.StartTs, "ix_statistics_start_ts");

                entity.HasIndex(e => new { e.MetadataId, e.StartTs }, "ix_statistics_statistic_id_start_ts").IsUnique();

                entity.Property(e => e.Id)
                    //.ValueGeneratedNever()
                    .HasColumnName("id");
                entity.Property(e => e.Created)
                    .HasColumnName("created");
                entity.Property(e => e.CreatedTs)
                    .HasColumnType("FLOAT")
                    .HasColumnName("created_ts");
                entity.Property(e => e.LastReset)
                    .HasColumnName("last_reset");
                entity.Property(e => e.LastResetTs)
                    .HasColumnType("FLOAT")
                    .HasColumnName("last_reset_ts");
                entity.Property(e => e.Max)
                    .HasColumnType("FLOAT")
                    .HasColumnName("max");
                entity.Property(e => e.Mean)
                    .HasColumnType("FLOAT")
                    .HasColumnName("mean");
                entity.Property(e => e.MeanWeight)
                    .HasColumnType("FLOAT")
                    .HasColumnName("mean_weight");
                entity.Property(e => e.MetadataId).HasColumnName("metadata_id");
                entity.Property(e => e.Min)
                    .HasColumnType("FLOAT")
                    .HasColumnName("min");
                entity.Property(e => e.Start)
                    .HasColumnName("start");
                entity.Property(e => e.StartTs)
                    .HasColumnType("FLOAT")
                    .HasColumnName("start_ts");
                entity.Property(e => e.State)
                    .HasColumnType("FLOAT")
                    .HasColumnName("state");
                entity.Property(e => e.Sum)
                    .HasColumnType("FLOAT")
                    .HasColumnName("sum");
            });

            modelBuilder.Entity<StatisticsShortTerm>(entity =>
            {
                entity.ToTable("statistics_short_term");
                entity.HasKey(k => k.Id);
                entity.HasIndex(e => e.StartTs, "ix_statistics_short_term_start_ts");

                entity.HasIndex(e => new { e.MetadataId, e.StartTs }, "ix_statistics_short_term_statistic_id_start_ts").IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");
                entity.Property(e => e.Created)
                    .HasColumnName("created");
                entity.Property(e => e.CreatedTs)
                    .HasColumnType("FLOAT")
                    .HasColumnName("created_ts");
                entity.Property(e => e.LastReset)
                    .HasColumnName("last_reset");
                entity.Property(e => e.LastResetTs)
                    .HasColumnType("FLOAT")
                    .HasColumnName("last_reset_ts");
                entity.Property(e => e.Max)
                    .HasColumnType("FLOAT")
                    .HasColumnName("max");
                entity.Property(e => e.Mean)
                    .HasColumnType("FLOAT")
                    .HasColumnName("mean");
                entity.Property(e => e.MeanWeight)
                    .HasColumnType("FLOAT")
                    .HasColumnName("mean_weight");
                entity.Property(e => e.MetadataId).HasColumnName("metadata_id");
                entity.Property(e => e.Min)
                    .HasColumnType("FLOAT")
                    .HasColumnName("min");
                entity.Property(e => e.Start)
                    .HasColumnName("start");
                entity.Property(e => e.StartTs)
                    .HasColumnType("FLOAT")
                    .HasColumnName("start_ts");
                entity.Property(e => e.State)
                    .HasColumnType("FLOAT")
                    .HasColumnName("state");
                entity.Property(e => e.Sum)
                    .HasColumnType("FLOAT")
                    .HasColumnName("sum");
            });
        }
    }
}
