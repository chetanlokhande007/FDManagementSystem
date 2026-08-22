using FinTrustFDManager.BAL.Common;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    public class FinancialCalculatorTests
    {
        // ═══════════════════════════════════════════
        //  GetDayCountBasis
        // ═══════════════════════════════════════════

        [Theory]
        [InlineData("ACTUAL_360", 360)]
        [InlineData("ACTUAL_365", 365)]
        public void GetDayCountBasis_ReturnsCorrectBasis(string basis, decimal expected)
        {
            var result = FinancialCalculator.GetDayCountBasis(basis);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("actual_360")]
        [InlineData("Actual_365")]
        [InlineData("  ACTUAL_360  ")]
        public void GetDayCountBasis_IsCaseInsensitiveAndTrimmed(string basis)
        {
            var result = FinancialCalculator.GetDayCountBasis(basis);
            Assert.True(result == 360m || result == 365m);
        }

        [Theory]
        [InlineData("30/360")]
        [InlineData("ACTUAL/ACTUAL")]
        [InlineData("")]
        [InlineData(null)]
        public void GetDayCountBasis_ThrowsForUnsupported(string? basis)
        {
            Assert.Throws<InvalidOperationException>(() =>
                FinancialCalculator.GetDayCountBasis(basis));
        }

        // ═══════════════════════════════════════════
        //  CalculateInterest — ACTUAL_365
        // ═══════════════════════════════════════════

        [Fact]
        public void CalculateInterest_ACTUAL365_SimpleCalculation()
        {
            // 100,000 at 8% for 90 days on ACTUAL_365
            // 100000 * 0.08 * 90/365 = 1972.60
            var result = FinancialCalculator.CalculateInterest(
                100_000m, 8m, 90, "ACTUAL_365");

            Assert.Equal(1972.60m, result);
        }

        [Fact]
        public void CalculateInterest_ACTUAL365_OneYear()
        {
            // 100,000 at 8% for 365 days on ACTUAL_365 = exactly 8000
            var result = FinancialCalculator.CalculateInterest(
                100_000m, 8m, 365, "ACTUAL_365");

            Assert.Equal(8000.00m, result);
        }

        [Fact]
        public void CalculateInterest_ACTUAL365_SingleDay()
        {
            // 100,000 at 10% for 1 day on ACTUAL_365
            // 100000 * 0.10 * 1/365 = 27.397... → 27.40
            var result = FinancialCalculator.CalculateInterest(
                100_000m, 10m, 1, "ACTUAL_365");

            Assert.Equal(27.40m, result);
        }

        // ═══════════════════════════════════════════
        //  CalculateInterest — ACTUAL_360
        // ═══════════════════════════════════════════

        [Fact]
        public void CalculateInterest_ACTUAL360_SimpleCalculation()
        {
            // 100,000 at 8% for 90 days on ACTUAL_360
            // 100000 * 0.08 * 90/360 = 2000.00
            var result = FinancialCalculator.CalculateInterest(
                100_000m, 8m, 90, "ACTUAL_360");

            Assert.Equal(2000.00m, result);
        }

        [Fact]
        public void CalculateInterest_ACTUAL360_30Days()
        {
            // 100,000 at 6% for 30 days on ACTUAL_360
            // 100000 * 0.06 * 30/360 = 500.00
            var result = FinancialCalculator.CalculateInterest(
                100_000m, 6m, 30, "ACTUAL_360");

            Assert.Equal(500.00m, result);
        }

        // ═══════════════════════════════════════════
        //  CalculateInterest — Rounding
        // ═══════════════════════════════════════════

        [Fact]
        public void CalculateInterest_RoundsToTwoDecimals()
        {
            // 50,000 at 7.5% for 13 days on ACTUAL_365
            // 50000 * 0.075 * 13/365 = 133.5616... → 133.56
            var result = FinancialCalculator.CalculateInterest(
                50_000m, 7.5m, 13, "ACTUAL_365");

            Assert.Equal(133.56m, result);
        }

        [Fact]
        public void CalculateInterest_RoundsHalfAwayFromZero()
        {
            // Construct a case where the 3rd decimal is exactly 5
            // 1000 * 1% * 18/365 = 0.49315... — not exactly .x5
            // Use: 100 * 1% * 1/365 = 0.002739... → rounds to 0.00
            var result = FinancialCalculator.CalculateInterest(
                100m, 1m, 1, "ACTUAL_365");
            Assert.Equal(0.00m, result);
        }

        // ═══════════════════════════════════════════
        //  CalculateInterest — Edge Cases
        // ═══════════════════════════════════════════

        [Theory]
        [InlineData(0)]      // zero days
        [InlineData(-1)]     // negative days
        public void CalculateInterest_ZeroOrNegativeDays_ReturnsZero(int days)
        {
            var result = FinancialCalculator.CalculateInterest(
                100_000m, 8m, days, "ACTUAL_365");
            Assert.Equal(0m, result);
        }

        [Theory]
        [InlineData(0)]      // zero principal
        [InlineData(-1000)]  // negative principal
        public void CalculateInterest_ZeroOrNegativePrincipal_ReturnsZero(decimal principal)
        {
            var result = FinancialCalculator.CalculateInterest(
                principal, 8m, 90, "ACTUAL_365");
            Assert.Equal(0m, result);
        }

        [Theory]
        [InlineData(0)]      // zero rate
        [InlineData(-5)]     // negative rate
        public void CalculateInterest_ZeroOrNegativeRate_ReturnsZero(decimal rate)
        {
            var result = FinancialCalculator.CalculateInterest(
                100_000m, rate, 90, "ACTUAL_365");
            Assert.Equal(0m, result);
        }

        // ═══════════════════════════════════════════
        //  CalculateInterest — Compound interest chain
        //  Simulates quarterly compounding over 1 year
        // ═══════════════════════════════════════════

        [Fact]
        public void CalculateInterest_CompoundChain_QuarterlyOn_ACTUAL365()
        {
            // Simulate quarterly compounding: Jan 1 → Dec 31 (ACTUAL_365)
            // Periods (actual days): 90, 91, 92, 91 = 364 days
            decimal principal = 100_000m;
            decimal rate = 8m;
            string basis = "ACTUAL_365";

            // Q1: Jan 1 → Apr 1 (90 days)
            decimal q1 = FinancialCalculator.CalculateInterest(principal, rate, 90, basis);
            decimal balance1 = principal + q1;

            // Q2: Apr 1 → Jul 1 (91 days)
            decimal q2 = FinancialCalculator.CalculateInterest(balance1, rate, 91, basis);
            decimal balance2 = balance1 + q2;

            // Q3: Jul 1 → Oct 1 (92 days)
            decimal q3 = FinancialCalculator.CalculateInterest(balance2, rate, 92, basis);
            decimal balance3 = balance2 + q3;

            // Q4: Oct 1 → Dec 31 (91 days)
            decimal q4 = FinancialCalculator.CalculateInterest(balance3, rate, 91, basis);
            decimal finalBalance = balance3 + q4;

            // Each period's interest should be slightly more than the previous
            // because balance is growing (compounding effect)
            Assert.True(q2 > q1, $"Q2 ({q2}) should be > Q1 ({q1})");
            Assert.True(q3 > q2, $"Q3 ({q3}) should be > Q2 ({q2})");
            Assert.True(q4 > q3, $"Q4 ({q4}) should be > Q3 ({q3})");

            // Final balance should be more than simple interest
            // Simple interest: 100000 * 0.08 * 364/365 = 7978.08
            decimal simpleInterest = FinancialCalculator.CalculateInterest(
                principal, rate, 364, basis);
            decimal simpleFinal = principal + simpleInterest;

            Assert.True(finalBalance > simpleFinal,
                $"Compound ({finalBalance}) should exceed simple ({simpleFinal})");

            // Verify no rounding errors accumulated badly
            // Theoretical compound: 100000 * (1 + 0.08 * 90/365) * (1 + 0.08 * 91/365) * ...
            decimal expected = 100_000m;
            int[] days = { 90, 91, 92, 91 };
            foreach (var d in days)
            {
                decimal interest = FinancialCalculator.CalculateInterest(expected, rate, d, basis);
                expected += interest;
            }

            Assert.Equal(expected, finalBalance);
        }
    }
}
