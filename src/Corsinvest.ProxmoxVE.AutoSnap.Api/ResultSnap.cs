using System.Collections.Generic;
using System.Linq;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// Execution Snap
    /// </summary>
    public class ResultSnap : ResultBaseSnap
    {
        /// <summary>
        /// Vms
        /// </summary>
        public List<ResultSnapVm> Vms { get; } = new List<ResultSnapVm>();

        /// <summary>
        /// Status
        /// </summary>
        /// <value></value>
        public override bool Status
        {
            get => !Vms.Any(a => !a.Status);
            internal set { }
        }
    }
}