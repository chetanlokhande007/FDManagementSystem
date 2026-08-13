using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class FDIdentificationService : IFDIdentificationService
    {
        private readonly FinTrustFDManager.DAL.Interfaces.IFDIdentificationRepository _repository;

        public FDIdentificationService(
            FinTrustFDManager.DAL.Interfaces.IFDIdentificationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<FDIdentification>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<FDIdentification?> GetByIdAsync(long id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<FDIdentification> CreateAsync(
            FDIdentification model)
        {
            model.Status = "DRAFT";
            model.CreatedDate = DateTime.UtcNow;

            return await _repository.AddAsync(model);
        }

        public async Task<FDIdentification?> UpdateAsync(
            long id,
            FDIdentification model)
        {
            model.FdId = id;

            return await _repository.UpdateAsync(model);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}