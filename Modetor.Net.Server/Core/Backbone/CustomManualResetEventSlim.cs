using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static IronPython.Modules.PythonThread;

namespace Modetor.Net.Server.Core.Backbone
{
    internal class CustomManualResetEventSlim
    {
        
        public void Dispose()
        {
            Look?.Dispose();
            Disposed = true;
        }

        public void Set()
        {
            try { Look.Set(); } catch { Init(); Set(); chances--; }
        }
        public void Reset()
        {
            try { Look.Reset(); } catch { Init(); Reset(); chances--; }
        }
        public void Wait()
        {
            try { Look.Wait(); } catch { Init(); Wait(); chances--; }
        }
        private void Init() { if(chances > 0) Look = new ManualResetEventSlim(false); }

        private int chances = 10;
        public bool Disposed { get; private set; }
        public ManualResetEventSlim Look { get; private set; }
    }
}
