using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class CountryRepository : ICountryRepository
    {
        private readonly ApplicationDbContext _context;

        public CountryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Country>> GetAllAsync()
        {
            return await _context.Countries
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Country?> GetByIdAsync(int id)
        {
            return await _context.Countries
                .FirstOrDefaultAsync(x => x.CountryId == id);
        }

        public async Task<Country?> GetByCodeAsync(string code)
        {
            return await _context.Countries
                .FirstOrDefaultAsync(x => x.CountryCode == code);
        }

        public async Task<Country> CreateAsync(Country country)
        {
            _context.Countries.Add(country);

            await _context.SaveChangesAsync();

            return country;
        }

        public async Task<Country> UpdateAsync(Country country)
        {
            _context.Countries.Update(country);

            await _context.SaveChangesAsync();

            return country;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(x => x.CountryId == id);

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
