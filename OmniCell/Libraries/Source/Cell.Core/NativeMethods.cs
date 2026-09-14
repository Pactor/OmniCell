using System.Threading;

namespace Cell.Core
{
    class NativeMethods
    {
        /// <summary>
        /// Gives the rest of the time slice to another ready thread. Thread.Yield calls
        /// SwitchToThread on Windows and sched_yield on Linux.
        /// </summary>
        public static void OsSwitchToThread()
        {
            Thread.Yield();
        }
    }
}
