using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FDIdentificationController : ControllerBase
    {
        private readonly IFDIdentificationService _service;

        public FDIdentificationController(IFDIdentificationService service)
        {
            _service = service;
        }

        // GET: api/FDIdentification
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        [HttpGet("landing")]
        public async Task<IActionResult> GetLandingData()
        {
            var result = await _service.GetLandingDataAsync();

            return Ok(result);
        }

        // GET: api/FDIdentification/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound("FD Identification not found.");

            return Ok(result);
        }

        // POST: api/FDIdentification
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FDIdentification model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.StartDate > model.EndDate)
                return BadRequest("Start date cannot be greater than end date.");

            if (model.PrincipalAmount <= 0)
                return BadRequest("Principal amount must be greater than zero.");

            var result = await _service.CreateAsync(model);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.FdId },
                result);
        }

        // PUT: api/FDIdentification/1
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] FDIdentification model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != model.FdId)
                return BadRequest("FD ID mismatch.");

            if (model.StartDate > model.EndDate)
                return BadRequest("Start date cannot be greater than end date.");

            if (model.PrincipalAmount <= 0)
                return BadRequest("Principal amount must be greater than zero.");

            var result = await _service.UpdateAsync(id, model);

            if (result == null)
                return NotFound("FD Identification not found.");

            return Ok(result);
        }

        // DELETE: api/FDIdentification/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound("FD Identification not found.");

            return Ok("FD Identification deleted successfully.");
        }
    }
}