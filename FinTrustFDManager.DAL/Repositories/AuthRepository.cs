using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Register(User user)
        {
            var emailExists = await _context.Users
                .AnyAsync(x => x.Email == user.Email);

            if (emailExists)
                return false;

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
