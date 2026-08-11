using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.BankAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class BankAccountController(IBankAccountService service) : ControllerBase
    {
        private readonly IBankAccountService _service = service;

        // GET: api/BankAccount
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var accounts =
                await _service.GetAllAsync();

            return Ok(accounts);
        }

        // GET: api/BankAccount/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var account =
                await _service.GetByIdAsync(id);

            if (account == null)
            {
                return NotFound(new
                {
                    message = "Bank account not found."
                });
            }

            return Ok(account);
        }

        // POST: api/BankAccount
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBankAccountDto dto)
        {
            try
            {
                var account =
                    await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = account.Id },
                    account);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/BankAccount/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBankAccountDto dto)
        {
            try
            {
                var account =
                    await _service.UpdateAsync(id, dto);

                if (account == null)
                {
                    return NotFound(new
                    {
                        message = "Bank account not found."
                    });
                }

                return Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/BankAccount/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted =
                await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Bank account not found."
                });
            }

            return Ok(new
            {
                message =
                    "Bank account deleted successfully."
            });
        }
    }
}
