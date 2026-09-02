using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.CoreData;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.EntityFrameworkCore;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.DAL.Data.Converters;
namespace FinTrustFDManager.DAL.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Country> Countries => Set<Country>();

        public DbSet<Currency> Currencies => Set<Currency>();

        public DbSet<Entity> Entities => Set<Entity>();
        public DbSet<FDInterest> FDInterests => Set<FDInterest>();


        public DbSet<CounterParty> CounterParties => Set<CounterParty>();

        public DbSet<Investment> Investments => Set<Investment>();

        public DbSet<InvestmentApproval> InvestmentApprovals
            => Set<InvestmentApproval>();

        public DbSet<CashFlow> CashFlows
            => Set<CashFlow>();

        public DbSet<InterestFrequency> InterestFrequencies
            => Set<InterestFrequency>();

        public DbSet<DayCountConvention> DayCountConventions
            => Set<DayCountConvention>();


        public DbSet<FDIdentification> FDIdentifications
     => Set<FDIdentification>();

        public DbSet<FDCashFlow> FDCashFlows
            => Set<FDCashFlow>();

        public DbSet<Benchmark> Benchmarks => Set<Benchmark>();
        public DbSet<BenchmarkRateHistory> BenchmarkRateHistories => Set<BenchmarkRateHistory>();

        public DbSet<FDApprovalHistory> FDApprovalHistories => Set<FDApprovalHistory>();

        public DbSet<FDAmendment> FDAmendments => Set<FDAmendment>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion(typeof(UtcDateTimeConverter));

            configurationBuilder
                .Properties<DateTime?>()
                .HaveConversion(typeof(NullableUtcDateTimeConverter));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Role { Id = 2, RoleName = "CA", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Role { Id = 3, RoleName = "Approver", IsActive = true, CreatedDate = DateTime.UtcNow }
            );

            modelBuilder.Entity<Entity>()
                .HasOne(x => x.Country)
                .WithMany(x => x.Entities)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<CounterParty>()
                .HasOne(x => x.Country)
                .WithMany(x => x.CounterParties)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Investment>()
                .HasOne(x => x.Entity)
                .WithMany()
                .HasForeignKey(x => x.EntityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Investment>()
                .HasOne(x => x.Country)
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Investment>()
                .HasOne(x => x.Currency)
                .WithMany()
                .HasForeignKey(x => x.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Investment>()
                .HasOne(x => x.InterestFrequency)
                .WithMany()
                .HasForeignKey(x => x.InterestFrequencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Investment>()
                .HasOne(x => x.DayCountConvention)
                .WithMany()
                .HasForeignKey(x => x.DayCountConventionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Investment>()
                .HasIndex(x => x.InvestmentReferenceNo)
                .IsUnique();

            modelBuilder.Entity<CashFlow>()
                .HasOne(x => x.Investment)
                .WithMany(x => x.CashFlows)
                .HasForeignKey(x => x.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvestmentApproval>()
                .HasOne(x => x.Investment)
                .WithMany(x => x.Approvals)
                .HasForeignKey(x => x.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);
            // FD Primary Keys
            // FD Identification Master FK relationships
            modelBuilder.Entity<FDIdentification>()
                .HasOne(x => x.Entity)
                .WithMany()
                .HasForeignKey(x => x.EntityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FDIdentification>()
                .HasOne(x => x.CounterParty)
                .WithMany()
                .HasForeignKey(x => x.CounterpartyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FDIdentification>()
                .HasOne(x => x.CurrencyNavigation)
                .WithMany()
                .HasForeignKey(x => x.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);


            // FD Primary Keys
            modelBuilder.Entity<FDIdentification>()
                .HasKey(x => x.FdId);

            modelBuilder.Entity<FDInterest>()
                .HasKey(x => x.FdInterestId);

            modelBuilder.Entity<FDCashFlow>()
                .HasKey(x => x.CashFlowId);


            // FDIdentification -> FDInterest (1 : 1)
            modelBuilder.Entity<FDInterest>()
                .HasOne(x => x.FDIdentification)
                .WithOne()
                .HasForeignKey<FDInterest>(x => x.FdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FDInterest>()
                .HasIndex(x => x.FdId)
                .IsUnique();

            // FDInterest -> InterestFrequency (Many : 1)
            modelBuilder.Entity<FDInterest>()
                .HasOne(x => x.InterestFrequency)
                .WithMany()
                .HasForeignKey(x => x.InterestFrequencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // FDInterest -> InterestFrequency (Compounding, optional)
            modelBuilder.Entity<FDInterest>()
                .HasOne(x => x.CompoundingFrequencyNavigation)
                .WithMany()
                .HasForeignKey(x => x.CompoundingFrequencyId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // FDInterest -> DayCountConvention (Many : 1)
            modelBuilder.Entity<FDInterest>()
                .HasOne(x => x.DayCountConvention)
                .WithMany()
                .HasForeignKey(x => x.DayCountConventionId)
                .OnDelete(DeleteBehavior.Restrict);

            // FDInterest -> Benchmark (optional, override existing)
            modelBuilder.Entity<FDInterest>()
                .HasOne(x => x.Benchmark)
                .WithMany()
                .HasForeignKey(x => x.BenchmarkId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // FDIdentification -> FDCashFlow (1 : Many)
            modelBuilder.Entity<FDCashFlow>()
                .HasOne<FDIdentification>()
                .WithMany()
                .HasForeignKey(x => x.FdId)
                .OnDelete(DeleteBehavior.Cascade);

            // FDIdentification -> FDApprovalHistory (1 : Many)
            modelBuilder.Entity<FDApprovalHistory>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<FDApprovalHistory>()
                .HasOne<FDIdentification>()
                .WithMany()
                .HasForeignKey(x => x.FdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FDApprovalHistory>()
                .HasIndex(x => new { x.FdId, x.ActionDate });

            // Unique constraint on FD reference number
            modelBuilder.Entity<FDIdentification>()
                .HasIndex(x => x.FdReferenceNo)
                .IsUnique();

            // Benchmark Master
            modelBuilder.Entity<Benchmark>()
                .HasKey(x => x.BenchmarkId);

            modelBuilder.Entity<Benchmark>()
                .HasIndex(x => x.BenchmarkName)
                .IsUnique();



            // FDAmendment -> FDIdentification (Many : 1)
            modelBuilder.Entity<FDAmendment>()
                .HasKey(x => x.AmendmentId);

            modelBuilder.Entity<FDAmendment>()
                .HasOne<FDIdentification>()
                .WithMany()
                .HasForeignKey(x => x.FdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FDAmendment>()
                .HasIndex(x => new { x.FdId, x.Status });

            modelBuilder.Entity<FDAmendment>()
                .HasIndex(x => x.RequestedBy);

            // Benchmark -> BenchmarkRateHistory (1 : Many)
            modelBuilder.Entity<BenchmarkRateHistory>()
                .HasKey(x => x.BenchmarkRateHistoryId);

            modelBuilder.Entity<BenchmarkRateHistory>()
                .HasOne(x => x.Benchmark)
                .WithMany(x => x.RateHistory)
                .HasForeignKey(x => x.BenchmarkId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InterestFrequency>().HasData(

                new InterestFrequency
                {
                    Id = 1,
                    FrequencyName = "Monthly"
                },

                new InterestFrequency
                {
                    Id = 2,
                    FrequencyName = "Quarterly"
                },

                new InterestFrequency
                {
                    Id = 3,
                    FrequencyName = "Half-Yearly"
                },

                new InterestFrequency
                {
                    Id = 4,
                    FrequencyName = "Annually"
                },

                new InterestFrequency
                {
                    Id = 5,
                    FrequencyName = "At Maturity"
                }
            );



            modelBuilder.Entity<DayCountConvention>().HasData(

                new DayCountConvention
                {
                    Id = 1,
                    ConventionName = "30/360"
                },

                new DayCountConvention
                {
                    Id = 2,
                    ConventionName = "Actual/360"
                },

                new DayCountConvention
                {
                    Id = 3,
                    ConventionName = "Actual/365"
                },

                new DayCountConvention
                {
                    Id = 4,
                    ConventionName = "Actual/Actual"
                }
            );

            // Seed Benchmark Master data
            modelBuilder.Entity<Benchmark>().HasData(
                new Benchmark
                {
                    BenchmarkId = 1,
                    BenchmarkName = "Repo Rate",
                    Description = "Reserve Bank of India Repo Rate",
                    CurrentRate = 6.50m,
                    RateUnit = "%",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Benchmark
                {
                    BenchmarkId = 2,
                    BenchmarkName = "LIBOR",
                    Description = "London Interbank Offered Rate",
                    CurrentRate = 5.50m,
                    RateUnit = "%",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Benchmark
                {
                    BenchmarkId = 3,
                    BenchmarkName = "MIBOR",
                    Description = "Mumbai Interbank Offered Rate",
                    CurrentRate = 6.25m,
                    RateUnit = "%",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Benchmark
                {
                    BenchmarkId = 4,
                    BenchmarkName = "T-Bill Rate",
                    Description = "Treasury Bill Rate",
                    CurrentRate = 6.00m,
                    RateUnit = "%",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Benchmark
                {
                    BenchmarkId = 5,
                    BenchmarkName = "SOFR",
                    Description = "Secured Overnight Financing Rate",
                    CurrentRate = 5.30m,
                    RateUnit = "%",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}