using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Entities.MasterData;
namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDInterestRepository
    {
        Task<IEnumerable<FDInterest>> GetAllAsync();

        Task<FDInterest?> GetByIdAsync(long id);

        Task<FDInterest?> GetByFdIdAsync(long fdId);

        Task<FDInterest> AddAsync(FDInterest model);

        Task<FDInterest?> UpdateAsync(FDInterest model);

        Task<bool> DeleteAsync(long id);

        Task<Benchmark?> GetBenchmarkByIdAsync(int benchmarkId);
    }
}
