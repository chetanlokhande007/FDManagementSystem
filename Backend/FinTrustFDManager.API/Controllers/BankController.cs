using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Bank;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class BankController : ControllerBase
    {
        private readonly IBankService _service;

        public BankController(IBankService service)
        {
            _service = service;
        }

        // GET: api/Bank
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var banks = await _service.GetAllAsync();

            return Ok(banks);
        }

        // GET: api/Bank/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var bank = await _service.GetByIdAsync(id);

            if (bank == null)
            {
                return NotFound(new
                {
                    message = "Bank not found."
                });
            }

            return Ok(bank);
        }

        // POST: api/Bank
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBankDto dto)
        {
            try
            {
                var bank = await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = bank.BankId },
                    bank);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/Bank/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBankDto dto)
        {
            try
            {
                var bank = await _service
                    .UpdateAsync(id, dto);

                if (bank == null)
                {
                    return NotFound(new
                    {
                        message = "Bank not found."
                    });
                }

                return Ok(bank);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/Bank/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Bank not found."
                });
            }

            return Ok(new
            {
                message = "Bank deleted successfully."
            });
        }
    }
}
