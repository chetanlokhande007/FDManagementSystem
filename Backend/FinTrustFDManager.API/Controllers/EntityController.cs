using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EntityController : ControllerBase
    {
        private readonly IEntityService _entityService;

        public EntityController(IEntityService entityService)
        {
            _entityService = entityService;
        }

        [HttpGet]
        public async Task<ActionResult<List<EntityDto>>> GetAll()
        {
            var entities = await _entityService.GetAllAsync();
            return Ok(entities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EntityDto>> GetById(int id)
        {
            var entity = await _entityService.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpPost]
        public async Task<ActionResult<EntityDto>> Create([FromBody] CreateEntityDto dto)
        {
            try
            {
                var created = await _entityService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.EntityId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EntityDto>> Update(int id, [FromBody] UpdateEntityDto dto)
        {
            try
            {
                var updated = await _entityService.UpdateAsync(id, dto);
                if (updated == null)
                {
                    return NotFound();
                }
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _entityService.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpPut("approve/{id}")]
        public async Task<ActionResult<EntityDto>> Approve(int id)
        {
            var entity = await _entityService.ApproveAsync(id);
            if (entity == null) return NotFound();
            
            return Ok(entity);
        }

        [HttpPut("reject/{id}")]
        public async Task<ActionResult<EntityDto>> Reject(int id)
        {
            var entity = await _entityService.RejectAsync(id);
            if (entity == null) return NotFound();
            
            return Ok(entity);
        }
    }
}
