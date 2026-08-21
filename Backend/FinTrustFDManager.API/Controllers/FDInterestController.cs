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

        [HttpGet("fd/{fdId:long}")]
        public async Task<IActionResult> GetByFdId(long fdId)
        {
            var data = await _service.GetByFdIdAsync(fdId);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FDInterest model)
        {
            try
            {
                var result = await _service.CreateAsync(model);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.FdInterestId },
                    result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            FDInterest model)
        {
            try
            {
                var result = await _service.UpdateAsync(id, model);

                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
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