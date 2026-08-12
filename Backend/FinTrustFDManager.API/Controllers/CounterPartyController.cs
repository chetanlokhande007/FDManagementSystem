using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.CounterParty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   // [Authorize]
    public class CounterPartyController : ControllerBase
    {
        private readonly ICounterPartyService _service;

        public CounterPartyController(
            ICounterPartyService service)
        {
            _service = service;
        }

        // GET: api/CounterParty
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var counterParties =
                await _service.GetAllAsync();

            return Ok(counterParties);
        }

        // GET: api/CounterParty/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var counterParty =
                await _service.GetByIdAsync(id);

            if (counterParty == null)
            {
                return NotFound(new
                {
                    message = "Counter Party not found."
                });
            }

            return Ok(counterParty);
        }

        // POST: api/CounterParty
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCounterPartyDto dto)
        {
            try
            {
                var counterParty =
                    await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new
                    {
                        id = counterParty.CounterPartyId
                    },
                    counterParty);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/CounterParty/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCounterPartyDto dto)
        {
            try
            {
                var counterParty =
                    await _service.UpdateAsync(id, dto);

                if (counterParty == null)
                {
                    return NotFound(new
                    {
                        message = "Counter Party not found."
                    });
                }

                return Ok(counterParty);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/CounterParty/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted =
                await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Counter Party not found."
                });
            }

            return Ok(new
            {
                message =
                    "Counter Party deleted successfully."
            });
        }
    }
}
