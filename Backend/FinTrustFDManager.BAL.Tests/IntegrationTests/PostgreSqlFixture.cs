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
