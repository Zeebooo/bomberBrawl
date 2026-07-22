using System.Collections.Generic;
using System.Linq;

namespace DevNotebook.Editor
{
    internal enum DevNotebookStartupAction
    {
        None,
        OpenGettingStarted,
        OpenNotebookWindow
    }

    internal readonly struct DevNotebookStartupDecision
    {
        public DevNotebookStartupDecision(DevNotebookStartupAction action, string notebookGuid)
        {
            Action = action;
            NotebookGuid = notebookGuid;
        }

        public DevNotebookStartupAction Action { get; }
        public string NotebookGuid { get; }
    }

    internal static class DevNotebookStartupLogic
    {
        public static DevNotebookStartupDecision DetermineAction(
            bool openOnLoad,
            bool showGettingStartedOnLoad,
            string lastOpenedNotebookGuid,
            IReadOnlyList<DevNotebookRecentItem> recentItems,
            IReadOnlyList<string> existingNotebookGuids)
        {
            if (!openOnLoad)
            {
                return new DevNotebookStartupDecision(DevNotebookStartupAction.None, string.Empty);
            }

            if (existingNotebookGuids == null || existingNotebookGuids.Count == 0)
            {
                return showGettingStartedOnLoad
                    ? new DevNotebookStartupDecision(DevNotebookStartupAction.OpenGettingStarted, string.Empty)
                    : new DevNotebookStartupDecision(DevNotebookStartupAction.None, string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(lastOpenedNotebookGuid) && existingNotebookGuids.Contains(lastOpenedNotebookGuid))
            {
                return new DevNotebookStartupDecision(DevNotebookStartupAction.OpenNotebookWindow, lastOpenedNotebookGuid);
            }

            string recentGuid = DevNotebookNotebookRepository.GetMostRecentExistingGuid(recentItems);
            if (!string.IsNullOrWhiteSpace(recentGuid) && existingNotebookGuids.Contains(recentGuid))
            {
                return new DevNotebookStartupDecision(DevNotebookStartupAction.OpenNotebookWindow, recentGuid);
            }

            return new DevNotebookStartupDecision(DevNotebookStartupAction.OpenNotebookWindow, existingNotebookGuids[0]);
        }
    }
}
