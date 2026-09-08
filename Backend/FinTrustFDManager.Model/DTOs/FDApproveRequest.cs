namespace FinTrustFDManager.Model.DTOs
{
    public class FDApproveRequest
    {
        /// <summary>
        /// Optional comments when approving an FD.
        /// Unlike rejection, approval does not require comments.
        /// </summary>
        public string? Comments { get; set; }
    }
}
