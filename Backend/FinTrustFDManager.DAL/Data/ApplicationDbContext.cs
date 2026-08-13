
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
            // FD Primary Keys
            modelBuilder.Entity<FDIdentification>()
                .HasKey(x => x.FdId);

            modelBuilder.Entity<FDInterest>()
                .HasKey(x => x.FdInterestId);

            modelBuilder.Entity<FDCashFlow>()
                .HasKey(x => x.CashFlowId);


            // FDIdentification -> FDInterest (1 : 1)
            modelBuilder.Entity<FDInterest>()
                .HasOne<FDIdentification>()
                .WithOne()
                .HasForeignKey<FDInterest>(x => x.FdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FDInterest>()
                .HasIndex(x => x.FdId)
                .IsUnique();

            // FDIdentification -> FDCashFlow (1 : Many)
            modelBuilder.Entity<FDCashFlow>()
                .HasOne<FDIdentification>()
                .WithMany()
                .HasForeignKey(x => x.FdId)
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