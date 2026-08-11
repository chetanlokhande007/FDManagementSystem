using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.CoreData;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Country> Countries => Set<Country>();

        public DbSet<Currency> Currencies => Set<Currency>();

        public DbSet<Entity> Entities => Set<Entity>();

        public DbSet<Bank> Banks => Set<Bank>();

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
                .HasOne(x => x.Bank)
                .WithMany()
                .HasForeignKey(x => x.BankId)
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