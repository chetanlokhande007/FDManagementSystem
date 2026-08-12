using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Country;
using FinTrustFDManager.Model.Entities.MasterData;
using FinTrustFDManager.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.BAL.Services
{
    public class CountryService : ICountryService
    {
        private readonly ApplicationDbContext _context;

        public CountryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CountryDto>> GetAllAsync()
        {
            return await _context.Countries
                .Select(c => new CountryDto
                {
                    CountryId = c.CountryId,
                    CountryCode = c.CountryCode,
                    CountryName = c.CountryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    ModifiedDate = c.ModifiedDate
                })
                .ToListAsync();
        }

        public async Task<CountryDto?> GetByIdAsync(int id)
        {
            return await _context.Countries
                .Where(c => c.CountryId == id)
                .Select(c => new CountryDto
                {
                    CountryId = c.CountryId,
                    CountryCode = c.CountryCode,
                    CountryName = c.CountryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    ModifiedDate = c.ModifiedDate
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CountryDto> CreateAsync(
            CreateCountryDto dto)
        {
            var codeExists = await _context.Countries
                .AnyAsync(c => c.CountryCode == dto.CountryCode);

            if (codeExists)
            {
                throw new InvalidOperationException(
                    "Country code already exists.");
            }

            var country = new Country
            {
                CountryCode = dto.CountryCode.Trim(),
                CountryName = dto.CountryName.Trim(),
                Description = dto.Description,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Countries.Add(country);

            await _context.SaveChangesAsync();

            return new CountryDto
            {
                CountryId = country.CountryId,
                CountryCode = country.CountryCode,
                CountryName = country.CountryName,
                Description = country.Description,
                IsActive = country.IsActive,
                CreatedDate = country.CreatedDate,
                ModifiedDate = country.ModifiedDate
            };
        }

        public async Task<CountryDto?> UpdateAsync(
            int id,
            UpdateCountryDto dto)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryId == id);

            if (country == null)
            {
                return null;
            }

            country.CountryCode = dto.CountryCode.Trim();
            country.CountryName = dto.CountryName.Trim();
            country.Description = dto.Description;
            country.IsActive = dto.IsActive;
            country.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryId == id);

            if (country == null)
            {
                return false;
            }

            _context.Countries.Remove(country);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
