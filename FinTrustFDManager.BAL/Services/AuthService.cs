using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs;
using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.BAL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repository;

        public AuthService(IAuthRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Register(RegisterDto dto)
        {
            User user = new User()
            {
                FullName = dto.FullName,
                Email = dto.Email,
                MobileNo = dto.MobileNo,
                Password = dto.Password
            };

            return await _repository.Register(user);
        }
    }
}
