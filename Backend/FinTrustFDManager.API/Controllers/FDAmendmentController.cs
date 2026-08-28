using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Amendment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinTrustFDManager.API.Controllers
{
    [ApiController]
    [Route("api/FDIdentification/{fdId:long}/amendments")]
    [Authorize]
    public class FDAmendmentController : ControllerBase
    {
        private readonly IFDAmendmentService _amendmentService;
        private readonly IFDIdentificationService _fdService;

        public FDAmendmentController(
            IFDAmendmentService amendmentService,
            IFDIdentificationService fdService)
        {
            _amendmentService = amendmentService;
            _fdService = fdService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !long.TryParse(claim.Value, out long userId))
                throw new UnauthorizedAccessException("User ID not found in token.");
            return userId;
        }

        // GET: api/FDIdentification/{fdId}/amendments
        [HttpGet]
        public async Task<IActionResult> GetAmendments(long fdId)
        {
            var fd = await _fdService.GetByIdAsync(fdId);
            if (fd == null) return NotFound($"FD with ID {fdId} not found.");

            var amendments = await _amendmentService.GetAmendmentsAsync(fdId);
            return Ok(amendments);
        }

        // GET: api/FDIdentification/{fdId}/amendments/{amendmentId}
        [HttpGet("{amendmentId:long}")]
        public async Task<IActionResult> GetAmendment(long fdId, long amendmentId)
        {
            var amendment = await _amendmentService.GetAmendmentByIdAsync(amendmentId);
            if (amendment == null || amendment.FdId != fdId)
                return NotFound($"Amendment {amendmentId} not found for FD {fdId}.");

            return Ok(amendment);
        }

        // POST: api/FDIdentification/{fdId}/amendments
        [HttpPost]
        [Authorize(Roles = "Admin,CA")]
        public async Task<IActionResult> RequestAmendment(long fdId, [FromBody] FDAmendmentRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var amendment = await _amendmentService.RequestAmendmentAsync(fdId, request, userId);
                return CreatedAtAction(nameof(GetAmendment), new { fdId, amendmentId = amendment.AmendmentId }, amendment);
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

        // POST: api/FDIdentification/{fdId}/amendments/{amendmentId}/approve
        [HttpPost("{amendmentId:long}/approve")]
        [Authorize(Roles = "Admin,Approver")]
        public async Task<IActionResult> ApproveAmendment(long fdId, long amendmentId, [FromBody] FDAmendmentActionDto? body)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _amendmentService.ApproveAmendmentAsync(fdId, amendmentId, userId, body?.Comments);
                return Ok(new { success = true, message = "Amendment approved and applied." });
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

        // POST: api/FDIdentification/{fdId}/amendments/{amendmentId}/reject
        [HttpPost("{amendmentId:long}/reject")]
        [Authorize(Roles = "Admin,Approver")]
        public async Task<IActionResult> RejectAmendment(long fdId, long amendmentId, [FromBody] FDAmendmentActionDto body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Comments))
                return BadRequest(new { success = false, message = "Rejection reason is required." });

            try
            {
                var userId = GetCurrentUserId();
                var result = await _amendmentService.RejectAmendmentAsync(fdId, amendmentId, userId, body.Comments);
                return Ok(new { success = true, message = "Amendment rejected." });
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
