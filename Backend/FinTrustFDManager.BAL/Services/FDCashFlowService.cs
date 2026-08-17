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
                CashFlowDate = x.CashFlowDate,
                CashFlowType = x.CashFlowType,
                Direction = x.Direction,
                Days = x.Days,
                OpeningBalance = x.OpeningBalance,
                ClosingBalance = x.ClosingBalance,
                PrincipalAmount = x.PrincipalAmount,
                GrossInterest = x.GrossInterest,
                TdsAmount = x.TdsAmount,
                NetInterest = x.NetInterest,
                TotalAmount = x.TotalAmount,
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
                CashFlowDate = x.CashFlowDate,
                CashFlowType = x.CashFlowType,
                Direction = x.Direction,
                Days = x.Days,
                OpeningBalance = x.OpeningBalance,
                ClosingBalance = x.ClosingBalance,
                PrincipalAmount = x.PrincipalAmount,
                GrossInterest = x.GrossInterest,
                TdsAmount = x.TdsAmount,
                NetInterest = x.NetInterest,
                TotalAmount = x.TotalAmount,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            };
        }

        // CREATE
        public async Task<FDCashFlowDto> CreateAsync(
            FDCashFlowDto dto)
        {
            var entity = new FDCashFlow
            {
                FdId = dto.FdId,
                CashFlowDate = dto.CashFlowDate,
                CashFlowType = dto.CashFlowType,
                Direction = dto.Direction,
                Days = dto.Days,
                OpeningBalance = dto.OpeningBalance,
                ClosingBalance = dto.ClosingBalance,
                PrincipalAmount = dto.PrincipalAmount,
                GrossInterest = dto.GrossInterest,
                TdsAmount = dto.TdsAmount,
                NetInterest = dto.NetInterest,
                TotalAmount = dto.TotalAmount,
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
            var existingCashFlows = (await _repository.GetByFdIdAsync(dto.FdId)).OrderBy(c => c.CashFlowDate).ToList();
            var targetIndex = existingCashFlows.FindIndex(c => c.CashFlowId == id);

            if (targetIndex == -1)
                return null;

            var fd = await _fdRepository.GetByIdAsync(dto.FdId);
            var interest = await _interestRepository.GetByFdIdAsync(dto.FdId);

            if (fd == null || interest == null)
                throw new InvalidOperationException("FD or Interest configuration not found.");

            var editedCashFlow = existingCashFlows[targetIndex];
            editedCashFlow.CashFlowDate = dto.CashFlowDate;
            editedCashFlow.GrossInterest = dto.GrossInterest;
            // Update other manually editable fields if needed
            editedCashFlow.NetInterest = dto.GrossInterest - editedCashFlow.TdsAmount;
            editedCashFlow.TotalAmount = editedCashFlow.NetInterest + editedCashFlow.PrincipalAmount;

            // Recalculate subsequent cash flows
            for (int i = targetIndex; i < existingCashFlows.Count; i++)
            {
                var current = existingCashFlows[i];

                if (i > 0)
                {
                    var prev = existingCashFlows[i - 1];
                    current.Days = (current.CashFlowDate - prev.CashFlowDate).Days;
                    
                    if (interest.IsCompounding)
                    {
                        current.OpeningBalance = prev.ClosingBalance;
                    }
                    else
                    {
                        current.OpeningBalance = fd.PrincipalAmount;
                    }

                    if (current.CashFlowType == "INTEREST")
                    {
                        if (i > targetIndex) // Only recalculate interest if it's not the manually edited row
                        {
                            decimal interestBase = interest.IsCompounding ? current.OpeningBalance : fd.PrincipalAmount;
                            decimal calculatedInterest = interestBase * (interest.InterestRate / 100m) * (current.Days / 365m);
                            current.GrossInterest = Math.Round(calculatedInterest, 2, MidpointRounding.AwayFromZero);
                            current.NetInterest = current.GrossInterest - current.TdsAmount;
                            current.TotalAmount = current.NetInterest;
                        }
                        
                        if (interest.IsCompounding)
                        {
                            current.ClosingBalance = current.OpeningBalance + current.GrossInterest;
                        }
                        else
                        {
                            current.ClosingBalance = current.OpeningBalance;
                        }
                    }
                    else if (current.CashFlowType == "MATURITY")
                    {
                        decimal maturityAmount = interest.IsCompounding
                            ? current.OpeningBalance
                            : fd.PrincipalAmount;
                            
                        current.ClosingBalance = maturityAmount;
                        current.TotalAmount = maturityAmount;
                    }
                }
            }

            await _repository.UpdateRangeAsync(existingCashFlows);

            return new FDCashFlowDto
            {
                CashFlowId = editedCashFlow.CashFlowId,
                FdId = editedCashFlow.FdId,
                CashFlowDate = editedCashFlow.CashFlowDate,
                CashFlowType = editedCashFlow.CashFlowType,
                Direction = editedCashFlow.Direction,
                Days = editedCashFlow.Days,
                OpeningBalance = editedCashFlow.OpeningBalance,
                ClosingBalance = editedCashFlow.ClosingBalance,
                PrincipalAmount = editedCashFlow.PrincipalAmount,
                GrossInterest = editedCashFlow.GrossInterest,
                TdsAmount = editedCashFlow.TdsAmount,
                NetInterest = editedCashFlow.NetInterest,
                TotalAmount = editedCashFlow.TotalAmount,
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
