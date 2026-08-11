using System;
using System.Collections.Generic;
using System.Text;

namespace FinTrustFDManager.Model.Entities.CoreData
{
    public class InvestmentApproval
    {
        public int InvestmentApprovalId { get; set; }

        
        public int InvestmentId { get; set; }

        
        public string Action { get; set; } = string.Empty;

        public int ActionBy { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        
        public string? Comments { get; set; }

       
        public Investment? Investment { get; set; }
    }
}
