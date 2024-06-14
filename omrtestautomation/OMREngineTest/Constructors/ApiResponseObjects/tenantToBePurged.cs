using System;

namespace OMREngineTest.Utils
{
    public struct tenantToBePurgedElement
    {        
        public string tenantId { get; set; }
		public string tenantName { get; set; }
		public DateTime disabledOn { get; set; }
		public TenantType tenantType { get; set; }
    }
}
