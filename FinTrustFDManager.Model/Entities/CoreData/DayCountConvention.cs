using System;
using System.Collections.Generic;
using System.Text;

namespace FinTrustFDManager.Model.Entities.CoreData
{
    public class DayCountConvention
    {
        public int Id { get; set; }

        public string ConventionName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
