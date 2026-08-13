using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FDInterestController : ControllerBase
    {
        private readonly IFDInterestService _service;

        public FDInterestController(IFDInterestService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();

            return Ok(data);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FDInterest model)
        {
            var result = await _service.CreateAsync(model);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.FdInterestId },
                result);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            FDInterest model)
        {
            var result = await _service.UpdateAsync(id, model);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}