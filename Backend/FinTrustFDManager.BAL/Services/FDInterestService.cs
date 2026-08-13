using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.BAL.Services
{
    public class FDInterestService : IFDInterestService
    {
        private readonly IFDInterestRepository _repository;

        public FDInterestService(IFDInterestRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<FDInterest>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<FDInterest?> GetByIdAsync(long id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<FDInterest> CreateAsync(FDInterest model)
        {
            model.CreatedDate = DateTime.UtcNow;

            return await _repository.AddAsync(model);
        }

        public async Task<FDInterest?> UpdateAsync(
            long id,
            FDInterest model)
        {
            model.FdInterestId = id;

            return await _repository.UpdateAsync(model);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}