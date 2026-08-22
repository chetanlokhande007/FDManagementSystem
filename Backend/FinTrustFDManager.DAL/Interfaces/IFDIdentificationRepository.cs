using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDIdentificationRepository
    {
        Task<IEnumerable<FDIdentification>> GetAllAsync();

        Task<FDIdentification?> GetByIdAsync(long id);

        Task<FDIdentification> AddAsync(FDIdentification model);

        Task<FDIdentification?> UpdateAsync(FDIdentification model);
        Task<FDIdentification?> GetLastAsync();
        Task<bool> DeleteAsync(long id);
        Task<bool> ChangeStatusAsync(long id, string status);

        /// <summary>
        /// Returns FD landing data in a single optimized query
        /// (joins FDIdentification + FDInterest + aggregated FDCashFlow).
        /// Replaces the N+1 query pattern.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetLandingDataAsync();
    }
}
