using FinTrustFDManager.Model.DTOs.DayCountConvention;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IDayCountConventionService
    {
        Task<List<DayCountConventionDto>> GetAllAsync();
        Task<DayCountConventionDto?> GetByIdAsync(int id);
        Task<DayCountConventionDto> CreateAsync(CreateDayCountConventionDto dto);
        Task<DayCountConventionDto?> UpdateAsync(int id, UpdateDayCountConventionDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
