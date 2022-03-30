using System.Runtime.InteropServices;
using Xunit;

namespace WagiRuby.Test
{
    public sealed class FactSkipIfWindowsAttribute : FactAttribute
    {
        public FactSkipIfWindowsAttribute()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                this.Skip = "Ruby Sample does not run on Windows.";
            }
        }
    }
}
