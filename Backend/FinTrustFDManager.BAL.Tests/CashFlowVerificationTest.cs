using FinTrustFDManager.BAL.Common;
using Xunit;
using System;

namespace FinTrustFDManager.BAL.Tests
{
    /// <summary>
    /// Verifies that our financial calculator produces the exact same numbers
    /// as shown in the reference image (FD-0088: ₹25,000, 3%, Monthly Interest,
    /// Half-Yearly Compounding, ACTUAL/360).
    /// </summary>
    public class CashFlowVerificationTest
    {
        /// <summary>
        /// Full row-by-row verification against the reference image (FD-0088).
        /// Parameters: ₹25,000, 3.00%, Monthly Interest, Half-Yearly Compounding, ACTUAL/360
        /// </summary>
        [Fact]
        public void ReferenceImage_FD0088_EachRowMatchesExactly()
        {
            decimal principal = 25_000m;
            decimal rate = 3m;
            string basis = "ACTUAL_360";
            decimal balance = principal;
            decimal accrued = 0;

            decimal Calc(int days) => FinancialCalculator.CalculateInterest(balance, rate, days, basis);

            // ── Row 1: FD Created ──
            // Opening=₹0.00, Interest=₹0.00, Closing=₹25,000.00, CashFlow=₹25,000.00
            // (No calculation needed — just the initial deposit)

            // ── Row 2: Interest, Jan 1→Feb 1, 31 days ──
            decimal i1 = Calc(31);
            Assert.Equal(64.58m, i1);           // Interest Amount
            accrued += i1;
            Assert.Equal(25_000m, balance);      // Opening Balance unchanged
            // CashFlowAmount = ₹64.58 (reference shows interest paid out)
            // ClosingBalance = ₹25,000.00 (balance unchanged in non-compounding month)

            // ── Row 3: Interest, Feb 1→Mar 1, 28 days ──
            decimal i2 = Calc(28);
            Assert.Equal(58.33m, i2);
            accrued += i2;

            // ── Row 4: Interest, Mar 1→Apr 1, 31 days ──
            decimal i3 = Calc(31);
            Assert.Equal(64.58m, i3);
            accrued += i3;

            // ── Row 5: Interest, Apr 1→May 1, 30 days ──
            decimal i4 = Calc(30);
            Assert.Equal(62.50m, i4);
            accrued += i4;

            // ── Row 6: Interest, May 1→Jun 1, 31 days ──
            decimal i5 = Calc(31);
            Assert.Equal(64.58m, i5);
            accrued += i5;

            // ── Check accrued after 5 monthly interest events ──
            // 64.58 + 58.33 + 64.58 + 62.50 + 64.58 = 314.57
            Assert.Equal(314.57m, accrued);

            // ── Row 7: Compounding Interest, Jun 1→Jul 1, 30 days ──
            // This is the Half-Yearly compounding event
            decimal i6 = Calc(30);
            Assert.Equal(62.50m, i6);
            accrued += i6;

            // Total accrued = 314.57 + 62.50 = 377.07
            Assert.Equal(377.07m, accrued);

            // Compound into balance
            balance += accrued;
            Assert.Equal(25_377.07m, balance);   // Matches reference Closing Balance
            accrued = 0;

            // ── Row 8: Interest, Jul 1→Aug 1, 31 days ──
            decimal i7 = Calc(31);
            Assert.Equal(65.56m, i7);            // Reference shows ₹65.56
            accrued += i7;

            // ── Row 9: Interest, Aug 1→Sep 1, 31 days ──
            decimal i8 = Calc(31);
            Assert.Equal(65.56m, i8);
            accrued += i8;

            // ── Row 10: Interest, Sep 1→Oct 1, 30 days ──
            decimal i9 = Calc(30);
            Assert.Equal(63.44m, i9);            // Reference shows ₹63.44
            accrued += i9;

            // ── Row 11: Interest, Oct 1→Nov 1, 31 days ──
            decimal i10 = Calc(31);
            Assert.Equal(65.56m, i10);
            accrued += i10;

            // ── Row 12: Interest, Nov 1→Dec 1, 30 days ──
            decimal i11 = Calc(30);
            Assert.Equal(63.44m, i11);
            accrued += i11;

            // ── Check accrued after 5 monthly interest events (second half) ──
            // 65.56 + 65.56 + 63.44 + 65.56 + 63.44 = 323.56
            Assert.Equal(323.56m, accrued);

            // ── Row 13: Compounding Interest, Dec 1→Dec 31, 30 days ──
            decimal i12 = Calc(30);
            Assert.Equal(63.44m, i12);           // Reference shows ₹63.44
            accrued += i12;

            // Total accrued second half = 323.56 + 63.44 = 387.00
            Assert.Equal(387.00m, accrued);

            // Compound into balance
            balance += accrued;
            Assert.Equal(25_764.07m, balance);    // Matches reference Closing Balance
            accrued = 0;

            // ── Row 14: Maturity ──
            // Opening=₹25,764.07, Interest=₹0.00, Closing=₹0.00, CashFlow=₹25,764.07
            decimal maturityAmount = balance;

            // ── Final totals ──
            decimal totalInterest = maturityAmount - principal;

            Assert.Equal(764.07m, totalInterest);     // Reference: Total Interest = ₹764.07
            Assert.Equal(25_764.07m, maturityAmount);  // Reference: Maturity Amount = ₹25,764.07
        }
    }
}
