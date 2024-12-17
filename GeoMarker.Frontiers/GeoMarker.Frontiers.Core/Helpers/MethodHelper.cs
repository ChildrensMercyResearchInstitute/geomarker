using System.Runtime.CompilerServices;

namespace GeoMarker.Frontiers.Core.Helpers
{
    public static class MethodHelper
    {
        public static string GetCurrentMethodName([CallerMemberName] string caller = null)
        {
            return caller;
        }
    }
}
