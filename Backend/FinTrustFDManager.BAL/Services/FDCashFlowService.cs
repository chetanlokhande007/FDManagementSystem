using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.BAL.Services
{
    public class FDCashFlowService : IFDCashFlowService
    {
        private readonly IFDCashFlowRepository _repository;
        private readonly IFDInterestRepository _interestRepository;
        private readonly IFDIdentificationRepository _fdRepository;

        public FDCashFlowService(
            IFDCashFlowRepository repository,
            IFDInterestRepository interestRepository,
            IFDIdentificationRepository fdRepository)
        {
            _repository = repository;
            _interestRepository = interestRepository;
            _fdRepository = fdRepository;
        }

        // GET ALL
        public async Task<IEnumerable<FDCashFlowDto>> GetAllAsync()
        {
            var cashFlows = await _repository.GetAllAsync();

            return cashFlows.Select(x => new FDCashFlowDto
            {
                CashFlowId = x.CashFlowId,
                FdId = x.FdId,
                Event = x.Event,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Days = x.Days,
                InterestRate = x.InterestRate,
                OpeningBalance = x.OpeningBalance,
                InterestAmount = x.InterestAmount,
                ClosingBalance = x.ClosingBalance,
                CashFlowAmount = x.CashFlowAmount,
                Direction = x.Direction,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            });
        }

        // GET BY ID
        public async Task<FDCashFlowDto?> GetByIdAsync(long id)
        {
            var x = await _repository.GetByIdAsync(id);

            if (x == null)
                return null;

            return new FDCashFlowDto
            {
                CashFlowId = x.CashFlowId,
                FdId = x.FdId,
                Event = x.Event,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Days = x.Days,
                InterestRate = x.InterestRate,
                OpeningBalance = x.OpeningBalance,
                InterestAmount = x.InterestAmount,
                ClosingBalance = x.ClosingBalance,
                CashFlowAmount = x.CashFlowAmount,
                Direction = x.Direction,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            };
        }

        // GET BY FD ID
        public async Task<IEnumerable<FDCashFlowDto>> GetByFdIdAsync(long fdId)
        {
            var cashFlows = await _repository.GetByFdIdAsync(fdId);

            return cashFlows.Select(x => new FDCashFlowDto
            {
                CashFlowId = x.CashFlowId,
                FdId = x.FdId,
                Event = x.Event,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Days = x.Days,
                InterestRate = x.InterestRate,
                OpeningBalance = x.OpeningBalance,
                InterestAmount = x.InterestAmount,
                ClosingBalance = x.ClosingBalance,
                CashFlowAmount = x.CashFlowAmount,
                Direction = x.Direction,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            });
        }

        // CREATE
        public async Task<FDCashFlowDto> CreateAsync(
            FDCashFlowDto dto)
        {
            var entity = new FDCashFlow
            {
                FdId = dto.FdId,
                Event = dto.Event,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Days = dto.Days,
                InterestRate = dto.InterestRate,
                OpeningBalance = dto.OpeningBalance,
                InterestAmount = dto.InterestAmount,
                ClosingBalance = dto.ClosingBalance,
                CashFlowAmount = dto.CashFlowAmount,
                Direction = dto.Direction,
                CurrencyCode = dto.CurrencyCode,
                Status = dto.Status,
                ReferenceNo = dto.ReferenceNo,
                CreatedDate = DateTime.UtcNow
            };

            var result =
                await _repository.CreateAsync(entity);

            dto.CashFlowId = result.CashFlowId;
            dto.CreatedDate = result.CreatedDate;

            return dto;
        }

        // UPDATE
        public async Task<FDCashFlowDto?> UpdateAsync(
            long id,
            FDCashFlowDto dto)
        {
            var existingCashFlows = (await _repository.GetByFdIdAsync(dto.FdId)).OrderBy(c => c.StartDate).ToList();
            var targetIndex = existingCashFlows.FindIndex(c => c.CashFlowId == id);

            if (targetIndex == -1)
                return null;

            var fd = await _fdRepository.GetByIdAsync(dto.FdId);
            var interest = await _interestRepository.GetByFdIdAsync(dto.FdId);

            if (fd == null || interest == null)
                throw new InvalidOperationException("FD or Interest configuration not found.");

            var editedCashFlow = existingCashFlows[targetIndex];
            
            // Validations
            if (dto.EndDate <= editedCashFlow.StartDate)
                throw new InvalidOperationException("End Date must be after Start Date.");
            
            if (dto.EndDate > fd.EndDate)
                throw new InvalidOperationException("End Date cannot exceed FD Maturity Date.");

            editedCashFlow.EndDate = dto.EndDate;
            // Recalculate Days for the edited cash flow
            editedCashFlow.Days = (editedCashFlow.EndDate.Date - editedCashFlow.StartDate.Date).Days;
            
            bool isCompounding = interest.IsCompounding;

            // Recalculate subsequent cash flows
            decimal accruedInterest = 0;
            
            for (int i = 0; i < existingCashFlows.Count; i++)
            {
                var current = existingCashFlows[i];

                if (i < targetIndex)
                {
                    // Before the edited row: just track compounding resets
                    if (isCompounding && current.Event == "Compounding Interest")
                    {
                        accruedInterest = 0;
                    }
                    else if (!isCompounding && current.Event == "Interest")
                    {
                        accruedInterest = 0;
                    }
                }
                else
                {
                    // At or after the edited row: recalculate chain
                    if (i > 0)
                    {
                        var prev = existingCashFlows[i - 1];
                        if (current.Event != "Maturity")
                        {
                            current.StartDate = prev.EndDate.Date;
                            if (current.EndDate < current.StartDate)
                            {
                                current.EndDate = current.StartDate;
                            }
                            current.Days = (current.EndDate.Date - current.StartDate.Date).Days;
                        }
                        current.OpeningBalance = prev.ClosingBalance;
                    }

                    if (current.Event == "Interest" || current.Event == "Compounding Interest")
                    {
                        if (current.Days > 0)
                        {
                            decimal periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                                current.OpeningBalance,
                                current.InterestRate,
                                current.Days,
                                interest.CalculationBasis);

                            if (isCompounding && current.Event == "Compounding Interest")
                            {
                                // Compounding event: add this period's interest to accrued, then compound all
                                accruedInterest += periodInterest;
                                current.InterestAmount = Math.Round(accruedInterest, 2);
                                current.ClosingBalance = current.OpeningBalance + Math.Round(accruedInterest, 2);
                                current.CashFlowAmount = 0;
                                accruedInterest = 0;
                            }
                            else if (isCompounding && current.Event == "Interest")
                            {
                                // Compounding mode, interest payment event: accrue interest (do NOT pay out)
                                accruedInterest += periodInterest;
                                current.InterestAmount = Math.Round(periodInterest, 2);
                                current.ClosingBalance = current.OpeningBalance;
                                current.CashFlowAmount = 0;
                            }
                            else if (!isCompounding && current.Event == "Interest")
                            {
                                // Non-compounding: interest is paid out each period
                                current.InterestAmount = Math.Round(periodInterest, 2);
                                current.ClosingBalance = current.OpeningBalance;
                                current.CashFlowAmount = Math.Round(periodInterest, 2);
                                accruedInterest = 0;
                            }
                            else
                            {
                                // Compounding event but not compounding mode: zero out
                                current.InterestAmount = 0;
                                current.ClosingBalance = current.OpeningBalance;
                                current.CashFlowAmount = 0;
                            }
                        }
                        else
                        {
                            // Zero-day period: no interest
                            current.InterestAmount = 0;
                            current.ClosingBalance = current.OpeningBalance;
                            current.CashFlowAmount = 0;
                        }
                    }
                    else if (current.Event == "Maturity")
                    {
                        current.ClosingBalance = 0;
                        current.CashFlowAmount = current.OpeningBalance;
                    }
                }
            }

            await _repository.UpdateRangeAsync(existingCashFlows);

            return new FDCashFlowDto
            {
                CashFlowId = editedCashFlow.CashFlowId,
                FdId = editedCashFlow.FdId,
                Event = editedCashFlow.Event,
                StartDate = editedCashFlow.StartDate,
                EndDate = editedCashFlow.EndDate,
                Days = editedCashFlow.Days,
                InterestRate = editedCashFlow.InterestRate,
                OpeningBalance = editedCashFlow.OpeningBalance,
                InterestAmount = editedCashFlow.InterestAmount,
                ClosingBalance = editedCashFlow.ClosingBalance,
                CashFlowAmount = editedCashFlow.CashFlowAmount,
                Direction = editedCashFlow.Direction,
                CurrencyCode = editedCashFlow.CurrencyCode,
                Status = editedCashFlow.Status,
                ReferenceNo = editedCashFlow.ReferenceNo,
                CreatedDate = editedCashFlow.CreatedDate
            };
        }

        // DELETE
        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
