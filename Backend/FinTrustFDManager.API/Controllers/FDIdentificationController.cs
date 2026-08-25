using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        private IActionResult? ValidateFD(FDIdentification model)
        {
            var errors = new Dictionary<string, string>();

            if (model.EntityId <= 0)
                errors["entityId"] = "Entity is required.";

            if (model.CounterpartyId <= 0)
                errors["counterpartyId"] = "Counterparty is required.";

            if (string.IsNullOrWhiteSpace(model.CurrencyCode))
                errors["currencyCode"] = "Transaction Currency is required.";

            if (model.PrincipalAmount <= 0)
                errors["principalAmount"] = "Principal Amount must be greater than 0.";

            if (model.StartDate == default)
                errors["startDate"] = "Start Date is required.";

            if (model.EndDate == default)
                errors["endDate"] = "End Date is required.";
            else if (model.EndDate <= model.StartDate)
                errors["endDate"] = "End Date must be after Start Date.";

            if (model.SettlementDate == default)
                errors["settlementDate"] = "Settlement Date is required.";
            else if (model.SettlementDate < model.EndDate)
                errors["settlementDate"] = "Settlement Date must be on or after End Date.";

            if (errors.Any())
            {
                return BadRequest(new { success = false, errors = errors });
            }

            return null;
        }

        // POST: api/FDIdentification
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FDIdentification model)
        {
            var validationResult = ValidateFD(model);
            if (validationResult != null)
                return validationResult;

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
            if (id != model.FdId)
                return BadRequest(new { success = false, errors = new { global = "FD ID mismatch." } });

            var validationResult = ValidateFD(model);
            if (validationResult != null)
                return validationResult;

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
                return NotFound(new { success = false, message = "FD Identification not found." });

            return Ok(new { success = true, message = "FD Identification deleted successfully." });
        }

        // PATCH: api/FDIdentification/1/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(long id, [FromBody] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status cannot be empty.");

            var result = await _service.ChangeStatusAsync(id, status);

            if (!result)
                return NotFound(new { success = false, message = "FD Identification not found." });

            return Ok(new { success = true, message = "Status updated successfully." });
        }
    }
}