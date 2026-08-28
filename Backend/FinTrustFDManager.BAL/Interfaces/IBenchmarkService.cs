using FinTrustFDManager.Model.Entities.MasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IBenchmarkService
    {
        Task<IEnumerable<Benchmark>> GetAllAsync();

        Task<Benchmark?> GetByIdAsync(int id);

        Task<Benchmark> CreateAsync(Benchmark model);

        Task<Benchmark?> UpdateAsync(int id, Benchmark model);

        Task<bool> DeleteAsync(int id);
    }
}
