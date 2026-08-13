using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDInterestService
    {
        Task<IEnumerable<FDInterest>> GetAllAsync();

        Task<FDInterest?> GetByIdAsync(long id);

        Task<FDInterest> CreateAsync(FDInterest model);

        Task<FDInterest?> UpdateAsync(long id, FDInterest model);

        Task<bool> DeleteAsync(long id);
    }
}
