using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool result = await _service.Register(dto);

            if (!result)
                return BadRequest("Email already exists.");

            return Ok("Registration Successful");
        }
    }
}