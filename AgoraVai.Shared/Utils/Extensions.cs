using System.Diagnostics;

namespace AgoraVai.Shared.Utils
{
    public static class Extensions
    {
        public static long ElapsedMilliseconds(this long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000 / Stopwatch.Frequency;
    }
}
