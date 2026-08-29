using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Amendment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class FDAmendmentService : IFDAmendmentService
    {
        private readonly IFDAmendmentRepository _amendmentRepository;
        private readonly IFDIdentificationRepository _fdRepository;
        private readonly IFDInterestRepository _interestRepository;
        private readonly IFDCashFlowRepository _cashFlowRepository;
        private readonly IFDInterestService _interestService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FDAmendmentService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public FDAmendmentService(
            IFDAmendmentRepository amendmentRepository,
            IFDIdentificationRepository fdRepository,
            IFDInterestRepository interestRepository,
            IFDCashFlowRepository cashFlowRepository,
            IFDInterestService interestService,
            IUnitOfWork unitOfWork,
            ILogger<FDAmendmentService> logger)
        {
            _amendmentRepository = amendmentRepository;
            _fdRepository = fdRepository;
            _interestRepository = interestRepository;
            _cashFlowRepository = cashFlowRepository;
            _interestService = interestService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<FDAmendment> RequestAmendmentAsync(long fdId, FDAmendmentRequestDto request, long requestedBy)
        {
            var fd = await _fdRepository.GetByIdAsync(fdId);
            if (fd == null)
                throw new KeyNotFoundException($"FD with ID {fdId} not found.");

            // Only APPROVED or ACTIVE FDs can be amended
            if (!FDStatus.IsProtected(fd.Status))
                throw new InvalidOperationException(
                    $"Cannot request amendment for FD '{fd.FdReferenceNo}' with status '{fd.Status}'. " +
                    $"Only APPROVED or ACTIVE FDs can be amended.");

            // Check for existing pending amendment
            var pending = await _amendmentRepository.GetPendingByFdIdAsync(fdId);
            if (pending != null)
                throw new InvalidOperationException(
                    $"FD '{fd.FdReferenceNo}' already has a pending amendment (ID: {pending.AmendmentId}). " +
                    $"Wait for it to be approved or rejected before requesting another.");

            // Capture original FD values
            var originalValues = CaptureFdValues(fd);

            // Capture original interest values if they exist
            var originalInterest = await _interestRepository.GetByFdIdAsync(fdId);
            if (originalInterest != null)
            {
                var interestDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(originalInterest, JsonOptions)) ?? new();
                originalValues["interest"] = interestDict;
            }

            // Build requested values (only non-null fields from the DTO)
            var requestedValues = BuildRequestedValues(request);

            if (requestedValues.Count == 0)
                throw new InvalidOperationException("No amendment values were provided. At least one field must be changed.");

            // Add interest fields if present
            if (request.InterestRateType != null || request.InterestRate != null ||
                request.BenchmarkId != null || request.Margin != null ||
                request.InterestFrequency != null || request.IsCompounding != null ||
                request.CompoundingFrequency != null || request.CalculationBasis != null)
            {
                var interestChanges = new Dictionary<string, object>();
                if (request.InterestRateType != null) interestChanges["interestRateType"] = request.InterestRateType;
                if (request.InterestRate != null) interestChanges["interestRate"] = request.InterestRate.Value;
                if (request.BenchmarkId != null) interestChanges["benchmarkId"] = request.BenchmarkId.Value;
                if (request.Margin != null) interestChanges["margin"] = request.Margin.Value;
                if (request.InterestFrequency != null) interestChanges["interestFrequency"] = request.InterestFrequency;
                if (request.IsCompounding != null) interestChanges["isCompounding"] = request.IsCompounding.Value;
                if (request.CompoundingFrequency != null) interestChanges["compoundingFrequency"] = request.CompoundingFrequency;
                if (request.CalculationBasis != null) interestChanges["calculationBasis"] = request.CalculationBasis;
                requestedValues["interest"] = interestChanges;
            }

            var amendment = new FDAmendment
            {
                FdId = fdId,
                Status = "PENDING_APPROVAL",
                Reason = request.Reason,
                OriginalValues = JsonSerializer.Serialize(originalValues, JsonOptions),
                RequestedValues = JsonSerializer.Serialize(requestedValues, JsonOptions),
                RequestedBy = requestedBy,
                RequestedDate = DateTime.UtcNow
            };

            var result = await _amendmentRepository.AddAsync(amendment);

            // Audit: Amendment requested
            await _fdRepository.AddApprovalHistoryAsync(new FDApprovalHistory
            {
                FdId = fdId,
                Action = FDAction.AmendmentRequest,
                FromStatus = fd.Status,
                ToStatus = fd.Status,
                ActionBy = requestedBy,
                ActionDate = DateTime.UtcNow,
                Comments = $"Amendment #{result.AmendmentId} requested: {request.Reason}",
                OldValues = amendment.OriginalValues,
                NewValues = amendment.RequestedValues
            });

            _logger.LogInformation(
                "Amendment {AmendmentId} requested for FD {FdReferenceNo} (ID={FdId}) by User {UserId}.",
                result.AmendmentId, fd.FdReferenceNo, fdId, requestedBy);

            return result;
        }

        public async Task<bool> ApproveAmendmentAsync(long fdId, long amendmentId, long approverUserId, string? comments = null)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment == null || amendment.FdId != fdId)
                throw new KeyNotFoundException($"Amendment {amendmentId} not found for FD {fdId}.");

            if (amendment.Status != "PENDING_APPROVAL")
                throw new InvalidOperationException(
                    $"Amendment {amendmentId} is not pending approval (current status: {amendment.Status}).");

            // Maker-Checker: requestor cannot approve their own amendment
            if (amendment.RequestedBy == approverUserId)
                throw new InvalidOperationException(
                    $"Maker-Check violation: User {approverUserId} requested this amendment and cannot approve their own.");

            var fd = await _fdRepository.GetByIdAsync(fdId);
            if (fd == null)
                throw new KeyNotFoundException($"FD with ID {fdId} not found.");

            // FD must still be in protected state
            if (!FDStatus.IsProtected(fd.Status))
                throw new InvalidOperationException(
                    $"FD '{fd.FdReferenceNo}' is no longer in a protected state ({fd.Status}). Amendment cannot be applied.");

            // Parse requested values
            var requestedValues = JsonSerializer.Deserialize<Dictionary<string, object>>(
                amendment.RequestedValues ?? "{}", JsonOptions) ?? new();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Apply FD changes
                ApplyFdChanges(fd, requestedValues);
                fd.ModifiedBy = approverUserId;
                fd.ModifiedDate = DateTime.UtcNow;
                await _fdRepository.UpdateAsync(fd);

                // Apply interest changes if present
                if (requestedValues.ContainsKey("interest"))
                {
                    var interestChanges = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        requestedValues["interest"].ToString() ?? "{}", JsonOptions) ?? new();

                    var existingInterest = await _interestRepository.GetByFdIdAsync(fdId);
                    if (existingInterest != null)
                    {
                        ApplyInterestChanges(existingInterest, interestChanges);
                        await _interestRepository.UpdateAsync(existingInterest);
                    }
                    else if (interestChanges.Count > 0)
                    {
                        // Create new interest if interest fields are being amended
                        var newInterest = new FDInterest
                        {
                            FdId = fdId,
                            InterestRateType = GetStringValue(interestChanges, "interestRateType") ?? "FIXED",
                            InterestRate = GetDecimalValue(interestChanges, "interestRate"),
                            InterestFrequency = GetStringValue(interestChanges, "interestFrequency") ?? "Monthly",
                            CompoundingFrequency = GetStringValue(interestChanges, "compoundingFrequency"),
                            IsCompounding = GetBoolValue(interestChanges, "isCompounding"),
                            CalculationBasis = GetStringValue(interestChanges, "calculationBasis") ?? "ACTUAL_365",
                            CreatedDate = DateTime.UtcNow
                        };
                        await _interestRepository.AddAsync(newInterest);
                    }
                }

                // Regenerate cashflows if financial fields changed
                bool financialFieldsChanged = requestedValues.ContainsKey("principalAmount") ||
                    requestedValues.ContainsKey("startDate") || requestedValues.ContainsKey("endDate") ||
                    requestedValues.ContainsKey("interest");

                if (financialFieldsChanged)
                {
                    await _interestService.RegenerateCashFlowsAsync(fdId);
                }

                // Mark amendment as approved
                amendment.Status = "APPROVED";
                amendment.ApprovedBy = approverUserId;
                amendment.ApprovedDate = DateTime.UtcNow;
                amendment.ApprovalComments = comments ?? "Approved";
                await _amendmentRepository.UpdateAsync(amendment);

                // Audit: Amendment approved
                await _fdRepository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = fdId,
                    Action = FDAction.AmendmentApprove,
                    FromStatus = fd.Status,
                    ToStatus = fd.Status,
                    ActionBy = approverUserId,
                    ActionDate = DateTime.UtcNow,
                    Comments = $"Amendment #{amendmentId} approved. {comments}",
                    OldValues = amendment.OriginalValues,
                    NewValues = amendment.RequestedValues
                });

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Amendment {AmendmentId} approved for FD {FdReferenceNo} (ID={FdId}) by User {UserId}.",
                    amendmentId, fd.FdReferenceNo, fdId, approverUserId);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> RejectAmendmentAsync(long fdId, long amendmentId, long approverUserId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments) || comments.Length < 5)
                throw new InvalidOperationException("Rejection reason is required (minimum 5 characters).");

            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment == null || amendment.FdId != fdId)
                throw new KeyNotFoundException($"Amendment {amendmentId} not found for FD {fdId}.");

            if (amendment.Status != "PENDING_APPROVAL")
                throw new InvalidOperationException(
                    $"Amendment {amendmentId} is not pending approval (current status: {amendment.Status}).");

            // Maker-Checker
            if (amendment.RequestedBy == approverUserId)
                throw new InvalidOperationException(
                    $"Maker-Check violation: User {approverUserId} requested this amendment and cannot reject their own.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                amendment.Status = "REJECTED";
                amendment.RejectedBy = approverUserId;
                amendment.RejectedDate = DateTime.UtcNow;
                amendment.RejectionComments = comments;
                await _amendmentRepository.UpdateAsync(amendment);

                // Audit: Amendment rejected
                var fd = await _fdRepository.GetByIdAsync(fdId);
                await _fdRepository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = fdId,
                    Action = FDAction.AmendmentReject,
                    FromStatus = fd?.Status,
                    ToStatus = fd?.Status,
                    ActionBy = approverUserId,
                    ActionDate = DateTime.UtcNow,
                    Comments = $"Amendment #{amendmentId} rejected: {comments}",
                    OldValues = amendment.OriginalValues,
                    NewValues = amendment.RequestedValues
                });

                // Original FD is NOT modified
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Amendment {AmendmentId} rejected for FD {FdId} by User {UserId}. Reason: {Comments}",
                    amendmentId, fdId, approverUserId, comments);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<FDAmendment>> GetAmendmentsAsync(long fdId)
        {
            return await _amendmentRepository.GetByFdIdAsync(fdId);
        }

        public async Task<FDAmendment?> GetAmendmentByIdAsync(long amendmentId)
        {
            return await _amendmentRepository.GetByIdAsync(amendmentId);
        }

        // ── Private helpers ──

        private static Dictionary<string, object> CaptureFdValues(FDIdentification fd)
        {
            return new Dictionary<string, object>
            {
                ["fdId"] = fd.FdId,
                ["fdReferenceNo"] = fd.FdReferenceNo,
                ["entityId"] = fd.EntityId,
                ["counterpartyId"] = fd.CounterpartyId,
                ["currencyCode"] = fd.Currency?.CurrencyCode ?? "INR",
                ["principalAmount"] = fd.PrincipalAmount,
                ["startDate"] = fd.StartDate,
                ["endDate"] = fd.EndDate,
                ["settlementDate"] = fd.SettlementDate,
                ["status"] = fd.Status
            };
        }

        private static Dictionary<string, object> BuildRequestedValues(FDAmendmentRequestDto request)
        {
            var values = new Dictionary<string, object>();
            if (request.PrincipalAmount.HasValue) values["principalAmount"] = request.PrincipalAmount.Value;
            if (request.CurrencyCode != null) values["currencyCode"] = request.CurrencyCode;
            if (request.StartDate.HasValue) values["startDate"] = request.StartDate.Value;
            if (request.EndDate.HasValue) values["endDate"] = request.EndDate.Value;
            if (request.SettlementDate.HasValue) values["settlementDate"] = request.SettlementDate.Value;
            if (request.CounterpartyId.HasValue) values["counterpartyId"] = request.CounterpartyId.Value;
            if (request.EntityId.HasValue) values["entityId"] = request.EntityId.Value;
            return values;
        }

        private static void ApplyFdChanges(FDIdentification fd, Dictionary<string, object> requestedValues)
        {
            if (requestedValues.ContainsKey("principalAmount"))
                fd.PrincipalAmount = Convert.ToDecimal(ToObject(requestedValues["principalAmount"]));
            if (requestedValues.ContainsKey("currencyCode"))
                fd.Currency?.CurrencyCode ?? "INR" = ToObject(requestedValues["currencyCode"]).ToString()!;
            if (requestedValues.ContainsKey("startDate"))
                fd.StartDate = DateTime.SpecifyKind(Convert.ToDateTime(ToObject(requestedValues["startDate"])), DateTimeKind.Utc);
            if (requestedValues.ContainsKey("endDate"))
                fd.EndDate = DateTime.SpecifyKind(Convert.ToDateTime(ToObject(requestedValues["endDate"])), DateTimeKind.Utc);
            if (requestedValues.ContainsKey("settlementDate"))
                fd.SettlementDate = DateTime.SpecifyKind(Convert.ToDateTime(ToObject(requestedValues["settlementDate"])), DateTimeKind.Utc);
            if (requestedValues.ContainsKey("counterpartyId"))
                fd.CounterpartyId = Convert.ToInt64(ToObject(requestedValues["counterpartyId"]));
            if (requestedValues.ContainsKey("entityId"))
                fd.EntityId = Convert.ToInt64(ToObject(requestedValues["entityId"]));
        }

        private static object ToObject(object value)
        {
            if (value is System.Text.Json.JsonElement element)
            {
                return element.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => element.GetString()!,
                    System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out var l) ? l : (object)element.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null!,
                    _ => element.ToString()!
                };
            }
            return value;
        }

        private static void ApplyInterestChanges(FDInterest interest, Dictionary<string, object> changes)
        {
            if (changes.ContainsKey("interestRateType"))
                interest.InterestRateType = changes["interestRateType"].ToString()!;
            if (changes.ContainsKey("interestRate"))
                interest.InterestRate = Convert.ToDecimal(changes["interestRate"]);
            if (changes.ContainsKey("benchmarkId"))
                interest.BenchmarkId = Convert.ToInt32(changes["benchmarkId"]);
            if (changes.ContainsKey("margin"))
                interest.Margin = Convert.ToDecimal(changes["margin"]);
            if (changes.ContainsKey("interestFrequency"))
                interest.InterestFrequencyId = changes["interestFrequency"].ToString()!;
            if (changes.ContainsKey("isCompounding"))
                interest.IsCompounding = Convert.ToBoolean(changes["isCompounding"]);
            if (changes.ContainsKey("compoundingFrequency"))
                interest.CompoundingFrequency = changes["compoundingFrequency"].ToString();
            if (changes.ContainsKey("calculationBasis"))
                interest.DayCountConvention?.ConventionName ?? "ACTUAL_365" = changes["calculationBasis"].ToString()!;
        }

        private static string? GetStringValue(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key].ToString() : null;
        }

        private static decimal GetDecimalValue(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) ? Convert.ToDecimal(dict[key]) : 0m;
        }

        private static bool GetBoolValue(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) && Convert.ToBoolean(dict[key]);
        }
    }
}

