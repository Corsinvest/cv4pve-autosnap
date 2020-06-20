using System;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// ResultBase
    /// </summary>
    public class ResultBaseSnap
    {
        private readonly Stopwatch _execution = new Stopwatch();

        internal void Start() => _execution.Start();
        internal void Stop() => _execution.Stop();

        /// <summary>
        /// Elapsed
        /// </summary>
        public TimeSpan Elapsed => _execution.Elapsed;

        /// <summary>
        /// Status
        /// </summary>
        /// <value></value>
        public virtual bool Status { get; internal set; }
    }
}