using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Currency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _service;

        public CurrencyController(
            ICurrencyService service)
        {
            _service = service;
        }

        // GET: api/Currency
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currencies = await _service.GetAllAsync();

            return Ok(currencies);
        }

        // GET: api/Currency/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currency = await _service.GetByIdAsync(id);

            if (currency == null)
            {
                return NotFound(new
                {
                    message = "Currency not found."
                });
            }

            return Ok(currency);
        }

        // POST: api/Currency
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCurrencyDto dto)
        {
            try
            {
                var currency = await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = currency.CurrencyId },
                    currency);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/Currency/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCurrencyDto dto)
        {
            try
            {
                var currency = await _service
                    .UpdateAsync(id, dto);

                if (currency == null)
                {
                    return NotFound(new
                    {
                        message = "Currency not found."
                    });
                }

                return Ok(currency);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/Currency/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Currency not found."
                });
            }

            return Ok(new
            {
                message = "Currency deleted successfully."
            });
        }
    }
}
