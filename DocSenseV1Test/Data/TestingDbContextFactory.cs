using DocSenseV1.Data;
using DocSenseV1.Models;
using Microsoft.EntityFrameworkCore;

namespace DocSenseV1Test.Data
{
    public class TestingDbContextFactory
    {
        public static EFDatabaseContext CreateContext(string? databaseName = null)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = Guid.NewGuid().ToString();
            }

            var options = new DbContextOptionsBuilder<EFDatabaseContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
            var context = new EFDatabaseContext(options);

            SeedData(context);

            return context;
        }

        private static void SeedData(EFDatabaseContext context)
        {
            if (!context.Plans.Any())
            {
                context.Plans.AddRange(
                    new PlanModel
                    {
                        Id = 1,
                        Type = "basic",
                        LimitSymbols = 100000,
                        LimitRequests = 1000,
                    },
                    new PlanModel
                    {
                        Id = 2,
                        Type = "pro",
                        LimitSymbols = 5000000,
                        LimitRequests = 50000,
                    }
                );
                context.SaveChanges();
            }

            if (!context.Usages.Any())
            {
                context.Usages.AddRange(
                    new UsageModel
                    {
                        Id = 1,
                        UserId = "user_1",
                        PlanId = 1, // free plan
                        TotalSymbols = 5000,
                        TotalRequests = 200,
                        PeriodStart = DateTime.UtcNow.Date,
                        CreatedAt = DateTime.UtcNow.Date
                    },
                    new UsageModel
                    {
                        Id = 2,
                        UserId = "user_1",
                        PlanId = 2, // pro plan
                        TotalSymbols = 5000,
                        TotalRequests = 200,
                        PeriodStart = DateTime.UtcNow.Date,
                        CreatedAt = DateTime.UtcNow.Date
                    }
                );
                context.SaveChanges();
            }


        }
    }
}
