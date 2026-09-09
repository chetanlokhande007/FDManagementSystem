using System;

namespace FinTrustFDManager.BAL.Common
{
    public static class FinancialCalculator
    {
        public static decimal CalculateInterest(
            decimal openingBalance, 
            decimal interestRate, 
            int days, 
            string calculationBasis)
        {
            if (days <= 0 || openingBalance <= 0 || interestRate <= 0)
                return 0;

            decimal dayCountBasis = GetDayCountBasis(calculationBasis);
            // Intermediate calculations use full precision, round only at the end.
            decimal calculatedInterest = openingBalance * (interestRate / 100m) * (days / dayCountBasis);
            
            return calculatedInterest; // Removed rounding here to preserve precision
        }

        public static int CalculateDays(DateTime startDate, DateTime endDate, string calculationBasis)
        {
            string basis = calculationBasis?.ToUpper()?.Trim() ?? "";

            if (basis == "30/360" || basis == "360/360")
            {
                int d1 = startDate.Day;
                int d2 = endDate.Day;

                if (d1 == 31) d1 = 30;
                if (d2 == 31 && d1 == 30) d2 = 30;

                return 360 * (endDate.Year - startDate.Year) + 30 * (endDate.Month - startDate.Month) + (d2 - d1);
            }
            
            return (endDate - startDate).Days;
        }

        public static decimal GetDayCountBasis(string? calculationBasis)
        {
            string basis = calculationBasis?.ToUpper()?.Trim() ?? "";

            if (basis == "ACTUAL_360" || basis == "ACTUAL/360")
            {
                return 360m;
            }
            else if (basis == "ACTUAL_365" || basis == "ACTUAL/365")
            {
                return 365m;
            }
            else if (basis == "30/360" || basis == "360/360")
            {
                return 360m;
            }
            else
            {
                // Default to 365 if unknown
                return 365m;
            }
        }
    }
}
