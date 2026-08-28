using FinTrustFDManager.Model.Entities.MasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IBenchmarkRepository
    {
        Task<IEnumerable<Benchmark>> GetAllAsync();

        Task<Benchmark?> GetByIdAsync(int id);

        Task<Benchmark> AddAsync(Benchmark model);

        Task<Benchmark?> UpdateAsync(Benchmark model);

        Task<bool> DeleteAsync(int id);
    }
}
