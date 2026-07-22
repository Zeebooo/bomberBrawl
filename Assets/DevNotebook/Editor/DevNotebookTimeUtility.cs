using System;

namespace DevNotebook.Editor
{
    internal static class DevNotebookTimeUtility
    {
        public static long NowTicksUtc()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}
