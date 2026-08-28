using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BenchmarkRateHistoryController : ControllerBase
    {
        private readonly IBenchmarkRateHistoryService _service;

        public BenchmarkRateHistoryController(IBenchmarkRateHistoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("benchmark/{benchmarkId:int}")]
        public async Task<IActionResult> GetByBenchmarkId(int benchmarkId)
        {
            var data = await _service.GetByBenchmarkIdAsync(benchmarkId);
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
        public async Task<IActionResult> Create([FromBody] BenchmarkRateHistory model)
        {
            try
            {
                var result = await _service.CreateAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = result.BenchmarkRateHistoryId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] BenchmarkRateHistory model)
        {
            try
            {
                var result = await _service.UpdateAsync(id, model);
                if (result == null)
                    return NotFound();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
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
