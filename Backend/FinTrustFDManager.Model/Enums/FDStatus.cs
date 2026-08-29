namespace FinTrustFDManager.Model.Enums
{
    public static class FDStatus
    {
        public const string Draft = "DRAFT";
        public const string PendingFdAdmin = "PENDING_FD_ADMIN";
        public const string FdAdminApproved = "FD_ADMIN_APPROVED";
        public const string PendingCa = "PENDING_CA";
        public const string Approved = "APPROVED";
        public const string Active = "ACTIVE";
        public const string Matured = "MATURED";
        public const string Rejected = "REJECTED";
        public const string FdAdminRejected = "FD_ADMIN_REJECTED";
        public const string CaRejected = "CA_REJECTED";
        public const string ReturnedToCreator = "RETURNED_TO_CREATOR";
        public const string AmendmentPending = "AMENDMENT_PENDING";

        /// <summary>
        /// Returns true if the FD is in a state that should not be modified.
        /// Includes terminal states (Approved, Active, Matured) and
        /// in-progress workflow states (Submitted, PendingApproval).
        /// </summary>
        public static bool IsProtected(string status)
        {
            return status == Approved || status == Active || status == Matured
                || status == PendingFdAdmin || status == PendingCa || status == FdAdminApproved;
        }

        /// <summary>
        /// Returns true if the FD is in an editable draft-like state.
        /// Only Draft and Rejected FDs can be directly edited.
        /// </summary>
        public static bool IsEditable(string status)
        {
            return status == Draft || status == Rejected || status == ReturnedToCreator || status == FdAdminRejected || status == CaRejected;
        }

        /// <summary>
        /// Returns all valid status values.
        /// </summary>
        public static bool IsValid(string status)
        {
            return status == Draft || status == PendingFdAdmin || status == FdAdminApproved || status == PendingCa
                || status == Approved || status == Active || status == Matured
                || status == Rejected || status == FdAdminRejected || status == CaRejected || status == ReturnedToCreator || status == AmendmentPending;
        }

        /// <summary>
        /// Returns all allowed target statuses for a given source status.
        /// </summary>
        public static IReadOnlyList<string> AllowedTransitions(string currentStatus)
        {
            return currentStatus switch
            {
                Draft => new[] { PendingFdAdmin },
                PendingFdAdmin => new[] { PendingCa, Approved, FdAdminRejected, ReturnedToCreator },
                PendingCa => new[] { Approved, CaRejected, ReturnedToCreator },
                Approved => new[] { Active, AmendmentPending },
                Active => new[] { Matured },
                // Rejected FDs can be re-edited and re-submitted
                Rejected => new[] { PendingFdAdmin },
                FdAdminRejected => new[] { PendingFdAdmin },
                CaRejected => new[] { PendingFdAdmin },
                ReturnedToCreator => new[] { PendingFdAdmin },
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
