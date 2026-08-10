using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.InvestmentApproval;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Services
{
    public class InvestmentApprovalService : IInvestmentApprovalService
    {
        private readonly IInvestmentApprovalRepository _repository;

        public InvestmentApprovalService(IInvestmentApprovalRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<InvestmentApprovalDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<InvestmentApprovalDto>> GetByInvestmentIdAsync(int investmentId)
        {
            var list = await _repository.GetByInvestmentIdAsync(investmentId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<InvestmentApprovalDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return MapToDto(entity);
        }

        public async Task<InvestmentApprovalDto> CreateAsync(CreateInvestmentApprovalDto dto)
        {
            var entity = new InvestmentApproval
            {
                InvestmentId = dto.InvestmentId,
                Action = dto.Action,
                ActionBy = dto.ActionBy,
                Comments = dto.Comments,
                ActionDate = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<InvestmentApprovalDto?> UpdateAsync(int id, UpdateInvestmentApprovalDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            entity.InvestmentId = dto.InvestmentId;
            entity.Action = dto.Action;
            entity.ActionBy = dto.ActionBy;
            entity.Comments = dto.Comments;

            var updated = await _repository.UpdateAsync(entity);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static InvestmentApprovalDto MapToDto(InvestmentApproval entity)
        {
            return new InvestmentApprovalDto
            {
                InvestmentApprovalId = entity.InvestmentApprovalId,
                InvestmentId = entity.InvestmentId,
                Action = entity.Action,
                ActionBy = entity.ActionBy,
                ActionDate = entity.ActionDate,
                Comments = entity.Comments
            };
        }
    }
}
