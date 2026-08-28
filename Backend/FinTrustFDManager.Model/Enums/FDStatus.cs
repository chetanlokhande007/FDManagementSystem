namespace FinTrustFDManager.Model.Enums
{
    public static class FDStatus
    {
        public const string Draft = "DRAFT";
        public const string Submitted = "SUBMITTED";
        public const string PendingApproval = "PENDING_APPROVAL";
        public const string Approved = "APPROVED";
        public const string Active = "ACTIVE";
        public const string Matured = "MATURED";
        public const string Rejected = "REJECTED";
        public const string AmendmentPending = "AMENDMENT_PENDING";

        /// <summary>
        /// Returns true if the FD is in a terminal state that should not be modified.
        /// </summary>
        public static bool IsProtected(string status)
        {
            return status == Approved || status == Active || status == Matured;
        }

        /// <summary>
        /// Returns true if the FD is in a editable draft-like state.
        /// </summary>
        public static bool IsEditable(string status)
        {
            return status == Draft || status == Rejected;
        }

        /// <summary>
        /// Returns all valid status values.
        /// </summary>
        public static bool IsValid(string status)
        {
            return status == Draft || status == Submitted || status == PendingApproval
                || status == Approved || status == Active || status == Matured
                || status == Rejected || status == AmendmentPending;
        }

        /// <summary>
        /// Returns all allowed target statuses for a given source status.
        /// </summary>
        public static IReadOnlyList<string> AllowedTransitions(string currentStatus)
        {
            return currentStatus switch
            {
                Draft => new[] { Submitted },
                Submitted => new[] { PendingApproval },
                PendingApproval => new[] { Approved, Rejected },
                Approved => new[] { Active, AmendmentPending },
                Active => new[] { Matured },
                // Rejected FDs can be re-edited and re-submitted
                Rejected => new[] { Submitted },
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// Validates that a transition from fromStatus to toStatus is allowed.
        /// Returns null if valid, or an error message if invalid.
        /// </summary>
        public static string? ValidateTransition(string fromStatus, string toStatus)
        {
            if (!IsValid(fromStatus))
                return $"Invalid current status '{fromStatus}'.";

            if (!IsValid(toStatus))
                return $"Invalid target status '{toStatus}'.";

            var allowed = AllowedTransitions(fromStatus);
            if (!allowed.Contains(toStatus))
            {
                return $"Invalid status transition from '{fromStatus}' to '{toStatus}'. " +
                       $"Allowed transitions: [{string.Join(", ", allowed)}].";
            }

            return null;
        }
    }
}
