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
            decimal calculatedInterest = openingBalance * (interestRate / 100m) * (days / dayCountBasis);
            
            return Math.Round(calculatedInterest, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal GetDayCountBasis(string? calculationBasis)
        {
            string basis = calculationBasis?.ToUpper()?.Trim() ?? "";

            if (basis == "ACTUAL_360")
            {
                return 360m;
            }
            else if (basis == "ACTUAL_365")
            {
                return 365m;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported CalculationBasis '{calculationBasis}'. Expected 'ACTUAL_360' or 'ACTUAL_365'.");
            }
        }
    }
}
