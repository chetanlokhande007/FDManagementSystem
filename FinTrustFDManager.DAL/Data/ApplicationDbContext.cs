using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.CoreData;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // User
        // =========================

        public DbSet<User> Users => Set<User>();


        // =========================
        // MASTER DATA
        // =========================

        public DbSet<Country> Countries => Set<Country>();

        public DbSet<Currency> Currencies => Set<Currency>();

        public DbSet<Entity> Entities => Set<Entity>();

        public DbSet<Bank> Banks => Set<Bank>();

        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

        public DbSet<CounterParty> CounterParties => Set<CounterParty>();


        // =========================
        // CORE DATA
        // =========================

        public DbSet<Investment> Investments => Set<Investment>();

        public DbSet<InvestmentApproval> InvestmentApprovals
            => Set<InvestmentApproval>();

        public DbSet<CashFlow> CashFlows
            => Set<CashFlow>();

        public DbSet<InterestFrequency> InterestFrequencies
            => Set<InterestFrequency>();

        public DbSet<DayCountConvention> DayCountConventions
            => Set<DayCountConvention>();


        // =========================
        // MODEL CONFIGURATION
        // =========================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entity>()
                .HasOne(x => x.Country)
                .WithMany(x => x.Entities)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bank>()
                .HasOne(x => x.Country)
                .WithMany(x => x.Banks)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bank>()
                .HasIndex(x => x.BankCode)
                .IsUnique();

            modelBuilder.Entity<CounterParty>()
                .HasOne(x => x.Country)
                .WithMany(x => x.CounterParties)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BankAccount>()
                .HasOne(x => x.Bank)
                .WithMany(x => x.BankAccounts)
                .HasForeignKey(x => x.BankId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BankAccount>()
                .HasOne(x => x.Currency)
                .WithMany(x => x.BankAccounts)
                .HasForeignKey(x => x.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BankAccount>()
                .HasIndex(x => x.AccountNumber)
                .IsUnique();


            // =========================
            // Interest Frequency Seed
            // =========================

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
                    FrequencyName = "Yearly"
                },

                new InterestFrequency
                {
                    Id = 5,
                    FrequencyName = "At Maturity"
                }
            );


            // =========================
            // Day Count Convention Seed
            // =========================

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
        }
    }
}