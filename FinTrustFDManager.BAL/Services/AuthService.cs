using FinTrustFDManager.BAL.DTOs.Auth;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs;
using FinTrustFDManager.Model.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinTrustFDManager.BAL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repository;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _repository.GetUserByEmailAsync(request.Email);

            if (existingUser != null)
                return false;

            var role = await _repository.GetRoleByIdAsync(request.RoleId);

            if (role == null)
                return false;

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                MobileNo = request.MobileNo,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = request.RoleId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _repository.AddUserAsync(user);

            return await _repository.SaveChangesAsync();
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _repository.GetUserByEmailAsync(request.Email);

            if (user == null || !user.IsActive)
                return null;

            bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

            if (!validPassword)
                return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role.RoleName
            };
        }
    }
}
