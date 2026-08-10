using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.CashFlow;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Services
{
    public class CashFlowService : ICashFlowService
    {
        private readonly ICashFlowRepository _repository;
        private readonly IInvestmentRepository _investmentRepository;

        public CashFlowService(ICashFlowRepository repository, IInvestmentRepository investmentRepository)
        {
            _repository = repository;
            _investmentRepository = investmentRepository;
        }

        public async Task<List<CashFlowDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<CashFlowDto>> GetByInvestmentIdAsync(int investmentId)
        {
            var list = await _repository.GetByInvestmentIdAsync(investmentId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<CashFlowDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return MapToDto(entity);
        }

        public async Task<CashFlowDto> CreateAsync(CreateCashFlowDto dto)
        {
            var investment = await _investmentRepository.GetByIdAsync(dto.InvestmentId);
            if (investment == null)
            {
                throw new InvalidOperationException($"Investment with ID {dto.InvestmentId} does not exist.");
            }

            var entity = new CashFlow
            {
                InvestmentId = dto.InvestmentId,
                CashFlowDate = dto.CashFlowDate,
                CashFlowType = dto.CashFlowType,
                PrincipalAmount = dto.PrincipalAmount,
                InterestAmount = dto.InterestAmount,
                TotalAmount = dto.TotalAmount,
                Status = "Pending",
                IsPaid = false,
                CreatedDate = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<CashFlowDto?> UpdateAsync(int id, UpdateCashFlowDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            var investment = await _investmentRepository.GetByIdAsync(dto.InvestmentId);
            if (investment == null)
            {
                throw new InvalidOperationException($"Investment with ID {dto.InvestmentId} does not exist.");
            }

            entity.InvestmentId = dto.InvestmentId;
            entity.CashFlowDate = dto.CashFlowDate;
            entity.CashFlowType = dto.CashFlowType;
            entity.PrincipalAmount = dto.PrincipalAmount;
            entity.InterestAmount = dto.InterestAmount;
            entity.TotalAmount = dto.TotalAmount;
            entity.Status = dto.Status;
            entity.IsPaid = dto.IsPaid;
            entity.PaidDate = dto.PaidDate;

            var updated = await _repository.UpdateAsync(entity);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static CashFlowDto MapToDto(CashFlow entity)
        {
            return new CashFlowDto
            {
                CashFlowId = entity.CashFlowId,
                InvestmentId = entity.InvestmentId,
                CashFlowDate = entity.CashFlowDate,
                CashFlowType = entity.CashFlowType,
                PrincipalAmount = entity.PrincipalAmount,
                InterestAmount = entity.InterestAmount,
                TotalAmount = entity.TotalAmount,
                Status = entity.Status,
                IsPaid = entity.IsPaid,
                PaidDate = entity.PaidDate,
                CreatedDate = entity.CreatedDate
            };
        }
    }
}
