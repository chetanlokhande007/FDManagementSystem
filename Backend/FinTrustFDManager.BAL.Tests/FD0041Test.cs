using FinTrustFDManager.BAL.Common;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Entities.CoreData;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace FinTrustFDManager.BAL.Tests
{
    public class FD0041Test
    {
        private readonly ITestOutputHelper _output;
        public FD0041Test(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void TestFD0041()
        {
            var fd = new FDIdentification
            {
                FdId = 1,
                PrincipalAmount = 45200m,
                StartDate = new DateTime(2026, 9, 3),
                EndDate = new DateTime(2027, 11, 24)
            };

            var interest = new FDInterest
            {
                InterestRateType = "FIXED",
                InterestRate = 5m,
                IsCompounding = true,
                PaymentConvention = "MATURITY",
                DayCountConvention = new DayCountConvention { ConventionName = "ACTUAL_365" },
                InterestFrequency = new InterestFrequency { FrequencyName = "MONTHLY" },
                CompoundingFrequencyNavigation = new InterestFrequency { FrequencyName = "MONTHLY" }
            };

            var cashFlows = FDScheduleEngine.GenerateSchedule(fd, interest);

            foreach (var cf in cashFlows)
            {
                _output.WriteLine($"{cf.StartDate:yyyy-MM-dd} to {cf.EndDate:yyyy-MM-dd} | {cf.Event} | Days: {cf.Days} | Open: {cf.OpeningBalance} | Int: {cf.InterestAmount} | Acc: {cf.AccruedInterest} | Cap: {cf.CapitalizedInterest} | Close: {cf.ClosingBalance} | Cash: {cf.CashFlowAmount}");
            }

            var maturityRow = cashFlows.Last(x => x.Event == "Maturity");
            _output.WriteLine($"Maturity Amount: {maturityRow.CashFlowAmount}");
            
            Assert.Equal(48048.17m, Math.Round(maturityRow.CashFlowAmount, 2));
        }
    }
}
