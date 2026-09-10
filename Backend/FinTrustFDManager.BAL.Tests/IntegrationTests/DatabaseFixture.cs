using FinTrustFDManager.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace FinTrustFDManager.BAL.Tests.IntegrationTests
{
    /// <summary>
    /// Provides an EF Core InMemory database for integration tests.
    /// Uses the same ApplicationDbContext model (configured for PostgreSQL in production)
    /// but runs against an in-memory provider for fast, isolated tests.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        private readonly string _dbName;
        public ApplicationDbContext Context { get; private set; }

        public DatabaseFixture()
        {
            // Unique database name per fixture instance for test isolation
            _dbName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;

            Context = new ApplicationDbContext(options);
            Context.Database.EnsureCreated();
            SeedMasterData(Context);
        }

        public static void SeedMasterData(ApplicationDbContext context)
        {
            if (!context.Countries.Any())
            {
                context.Countries.Add(new Model.Entities.MasterData.Country
                {
                    CountryId = 1,
                    CountryCode = "IND",
                    CountryName = "India",
                    Description = "India"
                });
                context.SaveChanges();
            }

            if (!context.Entities.Any())
            {
                context.Entities.Add(new Model.Entities.Entity
                {
                    EntityId = 1,
                    EntityCode = "ENT01",
                    EntityName = "Entity 1",
                    CountryId = 1
                });
                context.SaveChanges();
            }

            if (!context.CounterParties.Any())
            {
                context.CounterParties.Add(new Model.Entities.CounterParty
                {
                    CounterPartyId = 1,
                    CounterPartyCode = "CP01",
                    CounterPartyName = "Counterparty 1",
                    CountryId = 1
                });
                context.SaveChanges();
            }

            if (!context.Currencies.Any())
            {
                context.Currencies.Add(new Model.Entities.Currency
                {
                    CurrencyId = 1,
                    CurrencyCode = "INR",
                    CurrencyName = "Indian Rupee",
                    Symbol = "₹"
                });
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Creates a fresh DbContext connected to the same in-memory database.
        /// Useful for tests that need to avoid EF Core tracking conflicts.
        /// </summary>
        public ApplicationDbContext CreateFreshContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
