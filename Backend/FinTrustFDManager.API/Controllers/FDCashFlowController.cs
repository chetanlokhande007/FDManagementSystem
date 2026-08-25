using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FDCashFlowController : ControllerBase
    {
        private readonly IFDCashFlowService _service;

        public FDCashFlowController(
            IFDCashFlowService service)
        {
            _service = service;
        }

        // GET: api/FDCashFlow
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        // GET: api/FDCashFlow/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
            {
                return NotFound(new
                {
                    message = "FD Cash Flow not found."
                });
            }

            return Ok(data);
        }

        // GET: api/FDCashFlow/fd/1
        [HttpGet("fd/{fdId:long}")]
        public async Task<IActionResult> GetByFdId(long fdId)
        {
            var data = await _service.GetByFdIdAsync(fdId);

            if (data == null)
            {
                data = new List<FDCashFlowDto>();
            }

            return Ok(data);
        }

        // POST: api/FDCashFlow
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] FDCashFlowDto dto)
        {
            var result = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.CashFlowId },
                result);
        }

        // PUT: api/FDCashFlow/1
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] FDCashFlowDto dto)
        {
            if (id != dto.CashFlowId)
                return BadRequest(new { message = "Route ID does not match CashFlow ID." });

            var result =
                await _service.UpdateAsync(id, dto);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "FD Cash Flow not found."
                });
            }

            return Ok(result);
        }

        // DELETE: api/FDCashFlow/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted =
                await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "FD Cash Flow not found."
                });
            }

            return Ok(new
            {
                message = "FD Cash Flow deleted successfully."
            });
        }
    }
}
