namespace FinTrustFDManager.Model.DTOs.DayCountConvention
{
    public class DayCountConventionDto
    {
        public int Id { get; set; }
        public string ConventionName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
