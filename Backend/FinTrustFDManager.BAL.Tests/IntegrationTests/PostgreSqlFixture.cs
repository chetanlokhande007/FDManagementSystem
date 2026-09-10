using FinTrustFDManager.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FinTrustFDManager.BAL.Tests.IntegrationTests
{
    /// <summary>
    /// Provides a real PostgreSQL database for integration tests.
    /// Uses a dedicated test database that is created fresh and disposed after tests.
    /// </summary>
    public class PostgreSqlFixture : IAsyncLifetime
    {
        private const string BaseConnectionString = "Host=localhost;Port=5432;Database=FDManagementDB_Test;Username=postgres;Password=chetan1328";

        public ApplicationDbContext Context { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(BaseConnectionString)
                .Options;

            Context = new ApplicationDbContext(options);

            // Delete and recreate the test database fresh each time
            await Context.Database.EnsureDeletedAsync();
            await Context.Database.EnsureCreatedAsync();

            // Create the PostgreSQL sequence used by FDIdentificationRepository.GetNextFdReferenceNoAsync()
            await using var cmd = Context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "CREATE SEQUENCE IF NOT EXISTS fd_reference_seq START 1";
            await Context.Database.OpenConnectionAsync();
            await cmd.ExecuteNonQueryAsync();
            await Context.Database.CloseConnectionAsync();

            // Seed master data required by foreign keys
            var country = new Model.Entities.MasterData.Country
            {
                CountryId = 1,
                CountryCode = "IND",
                CountryName = "India",
                Description = "India"
            };
            var entity = new Model.Entities.Entity
            {
                EntityId = 1,
                EntityCode = "ENT01",
                EntityName = "Entity 1",
                CountryId = 1
            };
            var cp = new Model.Entities.CounterParty
            {
                CounterPartyId = 1,
                CounterPartyCode = "CP01",
                CounterPartyName = "Counterparty 1",
                CountryId = 1
            };
            var curr = new Model.Entities.Currency
            {
                CurrencyId = 1,
                CurrencyCode = "INR",
                CurrencyName = "Indian Rupee",
                Symbol = "₹"
            };

            Context.Countries.Add(country);
            Context.Entities.Add(entity);
            Context.CounterParties.Add(cp);
            Context.Currencies.Add(curr);
            await Context.SaveChangesAsync();

            // Synchronize PostgreSQL identity sequences so subsequent inserts don't collide
            await Context.Database.ExecuteSqlRawAsync(@"
                SELECT setval(pg_get_serial_sequence('""Countries""', 'CountryId'), COALESCE((SELECT MAX(""CountryId"") FROM ""Countries""), 1));
                SELECT setval(pg_get_serial_sequence('""Entities""', 'EntityId'), COALESCE((SELECT MAX(""EntityId"") FROM ""Entities""), 1));
                SELECT setval(pg_get_serial_sequence('""CounterParties""', 'CounterPartyId'), COALESCE((SELECT MAX(""CounterPartyId"") FROM ""CounterParties""), 1));
                SELECT setval(pg_get_serial_sequence('""Currencies""', 'CurrencyId'), COALESCE((SELECT MAX(""CurrencyId"") FROM ""Currencies""), 1));
            ");
        }

        public ApplicationDbContext CreateFreshContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(BaseConnectionString)
                .Options;
            return new ApplicationDbContext(options);
        }

        public async Task DisposeAsync()
        {
            if (Context != null)
            {
                await Context.Database.EnsureDeletedAsync();
                await Context.DisposeAsync();
            }
        }
    }
}
