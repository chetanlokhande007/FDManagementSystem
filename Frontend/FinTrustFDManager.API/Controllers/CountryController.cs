using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Country;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class CountryController : ControllerBase
    {
        private readonly ICountryService _service;

        public CountryController(ICountryService service)
        {
            _service = service;
        }

        // GET: api/Country
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var countries = await _service.GetAllAsync();

            return Ok(countries);
        }

        // GET: api/Country/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var country = await _service.GetByIdAsync(id);

            if (country == null)
            {
                return NotFound(new
                {
                    message = "Country not found."
                });
            }

            return Ok(country);
        }

        // POST: api/Country
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCountryDto dto)
        {
            try
            {
                var country = await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = country.CountryId },
                    country);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/Country/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCountryDto dto)
        {
            try
            {
                var country = await _service
                    .UpdateAsync(id, dto);

                if (country == null)
                {
                    return NotFound(new
                    {
                        message = "Country not found."
                    });
                }

                return Ok(country);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/Country/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Country not found."
                });
            }

            return Ok(new
            {
                message = "Country deleted successfully."
            });
        }
    }
}
