using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.BAL.Services
{
    public class FDCashFlowService : IFDCashFlowService
    {
        private readonly IFDCashFlowRepository _repository;

        public FDCashFlowService(
            IFDCashFlowRepository repository)
        {
            _repository = repository;
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
            var entity = new FDCashFlow
            {
                CashFlowId = id,
                FdId = dto.FdId,
                CashFlowDate = dto.CashFlowDate,
                CashFlowType = dto.CashFlowType,
                Direction = dto.Direction,
                PrincipalAmount = dto.PrincipalAmount,
                GrossInterest = dto.GrossInterest,
                TdsAmount = dto.TdsAmount,
                NetInterest = dto.NetInterest,
                TotalAmount = dto.TotalAmount,
                CurrencyCode = dto.CurrencyCode,
                Status = dto.Status,
                ReferenceNo = dto.ReferenceNo
            };

            var result =
                await _repository.UpdateAsync(entity);

            if (result == null)
                return null;

            dto.CashFlowId = result.CashFlowId;
            dto.CreatedDate = result.CreatedDate;

            return dto;
        }

        // DELETE
        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
