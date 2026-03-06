using DocSenseV1.Models;
using Microsoft.EntityFrameworkCore;

namespace DocSenseV1.Data
{
    public class EFDatabaseContext : DbContext
    {
        public EFDatabaseContext(DbContextOptions<EFDatabaseContext> options) : base(options)
        {
        }
        public DbSet<PlanModel> Plans { get; set; }
        public DbSet<UsageModel> Usages { get; set; }
        public DbSet<JobModel> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsageModel>()
                .HasIndex(u => new { u.UserId, u.PlanId })
                .IsUnique();

            modelBuilder.Entity<PlanModel>()
                .HasMany(p => p.Usages)
                .WithOne(u => u.Plan)
                .HasForeignKey(u => u.PlanId);

            modelBuilder.Entity<PlanModel>()
                .HasIndex(p => p.Type)
                .IsUnique();

            modelBuilder.Entity<PlanModel>().HasData(
                new PlanModel { Id = 1, Type = "basic", LimitRequests = 100, LimitSymbols = 100000 },
                new PlanModel { Id = 2, Type = "pro", LimitRequests = 1000, LimitSymbols = 1000000 },
                new PlanModel { Id = 3, Type = "ultra", LimitRequests = 10000, LimitSymbols = 10000000 }
            );
        }
    }
}
