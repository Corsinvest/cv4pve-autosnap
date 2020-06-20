using System.Linq;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// Execution Vm
    /// </summary>
    public class ResultSnapVm : ResultBaseSnap
    {
        /// <summary>
        /// Vm id
        /// </summary>
        /// <value></value>
        public int VmId { get; internal set; }
    }
}