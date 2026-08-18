using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.BAL.Services
{
    public class FDInterestService : IFDInterestService
    {
        private readonly IFDInterestRepository _interestRepository;
        private readonly IFDIdentificationRepository _fdRepository;
        private readonly IFDCashFlowRepository _cashFlowRepository;

        public FDInterestService(
            IFDInterestRepository interestRepository,
            IFDIdentificationRepository fdRepository,
            IFDCashFlowRepository cashFlowRepository)
        {
            _interestRepository = interestRepository;
            _fdRepository = fdRepository;
            _cashFlowRepository = cashFlowRepository;
        }

        public async Task<IEnumerable<FDInterest>> GetAllAsync()
        {
            return await _interestRepository.GetAllAsync();
        }

        public async Task<FDInterest?> GetByIdAsync(long id)
        {
            return await _interestRepository.GetByIdAsync(id);
        }

        public async Task<FDInterest?> GetByFdIdAsync(long fdId)
        {
            return await _interestRepository.GetByFdIdAsync(fdId);
        }

        public async Task<FDInterest> CreateAsync(FDInterest model)
        {
            var fd = await _fdRepository.GetByIdAsync(model.FdId);

            if (fd == null)
            {
                throw new KeyNotFoundException(
                    $"FD with ID {model.FdId} not found.");
            }

            var existing = await _interestRepository.GetByFdIdAsync(model.FdId);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Interest already exists for FD ID {model.FdId}.");
            }

            model.CreatedDate = DateTime.UtcNow;

            var interest = await _interestRepository.AddAsync(model);

            var cashFlows = GenerateCashFlows(fd, interest);

            await _cashFlowRepository.AddRangeAsync(cashFlows);

            return interest;
        }

        private List<FDCashFlow> GenerateCashFlows(
            FDIdentification fd,
            FDInterest interest)
        {
            var cashFlows = new List<FDCashFlow>();

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                CashFlowDate = fd.StartDate,
                CashFlowType = "PRINCIPAL",
                Direction = "OUTFLOW",
                Days = 0,
                OpeningBalance = 0,
                ClosingBalance = fd.PrincipalAmount,
                PrincipalAmount = fd.PrincipalAmount,
                GrossInterest = 0,
                TdsAmount = 0,
                NetInterest = 0,
                TotalAmount = fd.PrincipalAmount,
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = DateTime.UtcNow
            });

            decimal openingBalance = fd.PrincipalAmount;
            DateTime previousDate = fd.StartDate;
            
            string frequency = interest.IsCompounding 
                ? (interest.CompoundingFrequency ?? "QUARTERLY") 
                : interest.InterestFrequency;
            
            DateTime interestDate = GetNextDate(previousDate, frequency);

            while (interestDate <= fd.EndDate)
            {
                int days = (interestDate - previousDate).Days;
                
                decimal interestBase = interest.IsCompounding ? openingBalance : fd.PrincipalAmount;
                decimal calculatedInterest = interestBase * (interest.InterestRate / 100m) * (days / 365m);
                decimal roundedInterest = Math.Round(calculatedInterest, 2, MidpointRounding.AwayFromZero);
                
                decimal closingBalance;
                if (interest.IsCompounding)
                {
                    closingBalance = openingBalance + roundedInterest;
                }
                else
                {
                    closingBalance = openingBalance;
                }

                cashFlows.Add(new FDCashFlow
                {
                    FdId = fd.FdId,
                    CashFlowDate = interestDate,
                    CashFlowType = "INTEREST",
                    Direction = "INFLOW",
                    Days = days,
                    OpeningBalance = interestBase,
                    ClosingBalance = closingBalance,
                    PrincipalAmount = 0,
                    GrossInterest = roundedInterest,
                    TdsAmount = 0,
                    NetInterest = roundedInterest,
                    TotalAmount = roundedInterest,
                    CurrencyCode = fd.CurrencyCode ?? "INR",
                    Status = "PENDING",
                    ReferenceNo = fd.FdReferenceNo ?? "",
                    CreatedDate = DateTime.UtcNow
                });

                if (interest.IsCompounding)
                {
                    openingBalance = closingBalance;
                }
                else
                {
                    openingBalance = fd.PrincipalAmount;
                }

                previousDate = interestDate;
                interestDate = GetNextDate(interestDate, frequency);
            }

            // Calculate broken period interest if there are remaining days
            if (previousDate < fd.EndDate)
            {
                int remainingDays = (fd.EndDate - previousDate).Days;
                if (remainingDays > 0)
                {
                    decimal interestBase = interest.IsCompounding ? openingBalance : fd.PrincipalAmount;
                    decimal brokenPeriodInterest = interestBase * (interest.InterestRate / 100m) * (remainingDays / 365m);
                    decimal finalInterest = Math.Round(brokenPeriodInterest, 2, MidpointRounding.AwayFromZero);
                    
                    decimal closingBalance;
                    if (interest.IsCompounding)
                    {
                        closingBalance = openingBalance + finalInterest;
                    }
                    else
                    {
                        closingBalance = openingBalance;
                    }

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        CashFlowDate = fd.EndDate,
                        CashFlowType = "INTEREST",
                        Direction = "INFLOW",
                        Days = remainingDays,
                        OpeningBalance = interestBase,
                        ClosingBalance = closingBalance,
                        PrincipalAmount = 0,
                        GrossInterest = finalInterest,
                        TdsAmount = 0,
                        NetInterest = finalInterest,
                        TotalAmount = finalInterest,
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = DateTime.UtcNow
                    });

                    if (interest.IsCompounding)
                    {
                        openingBalance = closingBalance;
                    }
                    else
                    {
                        openingBalance = fd.PrincipalAmount;
                    }
                }
            }

            // Maturity
            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                CashFlowDate = fd.EndDate,
                CashFlowType = "MATURITY",
                Direction = "INFLOW",
                Days = 0,
                OpeningBalance = openingBalance,
                ClosingBalance = openingBalance, // Usually principal is returned
                PrincipalAmount = fd.PrincipalAmount,
                GrossInterest = 0,
                TdsAmount = 0,
                NetInterest = 0,
                TotalAmount = fd.PrincipalAmount,
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = DateTime.UtcNow
            });

            return cashFlows;
        }

        public async Task<FDInterest?> UpdateAsync(
            long id,
            FDInterest model)
        {
            model.FdInterestId = id;

            var updatedInterest = await _interestRepository.UpdateAsync(model);

            if (updatedInterest != null)
            {
                var fd = await _fdRepository.GetByIdAsync(updatedInterest.FdId);
                if (fd != null)
                {
                    // Get existing cashflows
                    var existingCashFlows = await _cashFlowRepository.GetByFdIdAsync(fd.FdId);
                    
                    // Delete existing cashflows
                    foreach (var cf in existingCashFlows)
                    {
                        await _cashFlowRepository.DeleteAsync(cf.CashFlowId);
                    }

                    // Generate and save new cashflows
                    var newCashFlows = GenerateCashFlows(fd, updatedInterest);
                    await _cashFlowRepository.AddRangeAsync(newCashFlows);
                }
            }

            return updatedInterest;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _interestRepository.DeleteAsync(id);
        }

        private DateTime GetNextDate(DateTime currentDate, string frequency)
        {
            return (frequency?.ToUpper() ?? "") switch
            {
                "MONTHLY" => currentDate.AddMonths(1),
                "QUARTERLY" => currentDate.AddMonths(3),
                "HALF_YEARLY" => currentDate.AddMonths(6),
                "ANNUALLY" => currentDate.AddYears(1),
                _ => currentDate.AddMonths(3) // Default to quarterly if empty/unknown
            };
        }
    }
}