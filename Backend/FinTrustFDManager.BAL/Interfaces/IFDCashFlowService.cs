using FinTrustFDManager.BAL.DTOs;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDCashFlowService
    {
        Task<IEnumerable<FDCashFlowDto>> GetAllAsync();

        Task<FDCashFlowDto?> GetByIdAsync(long id);

        Task<IEnumerable<FDCashFlowDto>> GetByFdIdAsync(long fdId);

        Task<FDCashFlowDto> CreateAsync(FDCashFlowDto dto);

        Task<FDCashFlowDto?> UpdateAsync(
            long id,
            FDCashFlowDto dto);

        Task<bool> DeleteAsync(long id);
    }
}
