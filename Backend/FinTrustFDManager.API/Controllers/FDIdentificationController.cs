using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !long.TryParse(claim.Value, out long userId))
                throw new UnauthorizedAccessException("User ID not found in token.");
            return userId;
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

            if (model.CurrencyId <= 0)
                errors["currencyId"] = "Transaction Currency is required.";

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

        // GET: api/FDIdentification/1/approval-history
        [HttpGet("{id}/approval-history")]
        public async Task<IActionResult> GetApprovalHistory(long id)
        {
            var result = await _service.GetApprovalHistoryAsync(id);
            return Ok(result);
        }

        // POST: api/FDIdentification
        [HttpPost]
        [Authorize(Roles = "Admin,CA")]
        public async Task<IActionResult> Create([FromBody] FDIdentification model)
        {
            var validationResult = ValidateFD(model);
            if (validationResult != null) return validationResult;
            model.CreatedBy = GetCurrentUserId();
            var result = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = result.FdId }, result);
        }

        // PUT: api/FDIdentification/1
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CA")]
        public async Task<IActionResult> Update(long id, [FromBody] FDIdentification model)
        {
            if (id != model.FdId) return BadRequest(new { success = false, errors = new { global = "FD ID mismatch." } });
            var validationResult = ValidateFD(model);
            if (validationResult != null) return validationResult;
            model.ModifiedBy = GetCurrentUserId();
            var result = await _service.UpdateAsync(id, model);
            if (result == null) return NotFound("FD Identification not found.");
            return Ok(result);
        }

        // DELETE: api/FDIdentification/1
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,CA")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound(new { success = false, message = "FD Identification not found." });
            return Ok(new { success = true, message = "FD Identification deleted successfully." });
        }

        // POST: api/FDIdentification/1/submit
        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Admin,CA")]
        public async Task<IActionResult> Submit(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _service.SubmitAsync(id, userId);
                return Ok(new { success = true, message = "FD submitted for approval." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        // POST: api/FDIdentification/1/approve
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Approver")]
        public async Task<IActionResult> Approve(long id, [FromBody] FDRejectRequest? body)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _service.ApproveAsync(id, userId, body?.Comments);
                return Ok(new { success = true, message = "FD approved successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        // POST: api/FDIdentification/1/reject
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,Approver")]
        public async Task<IActionResult> Reject(long id, [FromBody] FDRejectRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Comments))
                return BadRequest(new { success = false, message = "Rejection reason is required." });
            try
            {
                var userId = GetCurrentUserId();
                await _service.RejectAsync(id, userId, body.Comments);
                return Ok(new { success = true, message = "FD rejected." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }
    }
}