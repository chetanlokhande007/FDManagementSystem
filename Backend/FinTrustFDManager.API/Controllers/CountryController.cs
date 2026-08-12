using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Country;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly ICountryService _countryService;

        public CountryController(
            ICountryService countryService)
        {
            _countryService = countryService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CountryDto>>> GetAll()
        {
            var countries =
                await _countryService.GetAllAsync();

            return Ok(countries);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CountryDto>> GetById(
            int id)
        {
            var country =
                await _countryService.GetByIdAsync(id);

            if (country == null)
            {
                return NotFound();
            }

            return Ok(country);
        }

        [HttpPost]
        public async Task<ActionResult<CountryDto>> Create(
            [FromBody] CreateCountryDto dto)
        {
            try
            {
                var country =
                    await _countryService.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = country.CountryId },
                    country);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CountryDto>> Update(
            int id,
            [FromBody] UpdateCountryDto dto)
        {
            try
            {
                var country =
                    await _countryService.UpdateAsync(id, dto);

                if (country == null)
                {
                    return NotFound();
                }

                return Ok(country);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success =
                await _countryService.DeleteAsync(id);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
