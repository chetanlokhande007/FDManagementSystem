using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities.Investment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDIdentificationService
    {
        Task<IEnumerable<FDIdentification>> GetAllAsync();

        Task<FDIdentification?> GetByIdAsync(long id);

        Task<FDIdentification> CreateAsync(FDIdentification model);

        Task<FDIdentification?> UpdateAsync(long id, FDIdentification model);

        Task<bool> DeleteAsync(long id);

        // Landing Page
        Task<IEnumerable<FDLandingDto>> GetLandingDataAsync();

        Task<bool> ChangeStatusAsync(long id, string status);
    }
}