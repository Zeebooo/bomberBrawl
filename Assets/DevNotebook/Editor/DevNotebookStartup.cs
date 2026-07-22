using UnityEditor;
using UnityEngine;

namespace DevNotebook.Editor
{
    [InitializeOnLoad]
    internal static class DevNotebookStartup
    {
        private const string StartupSessionKey = "DevNotebook.StartupRan";

        static DevNotebookStartup()
        {
            if (SessionState.GetBool(StartupSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(StartupSessionKey, true);
            EditorApplication.delayCall += RunOnceAfterLoad;
        }

        private static void RunOnceAfterLoad()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunOnceAfterLoad;
                return;
            }

            DevNotebookProjectSettings settings = DevNotebookProjectSettings.instance;
            settings.PruneInvalidEntries();

            var notebookGuids = DevNotebookNotebookRepository.FindNotebookGuids();
            DevNotebookStartupDecision decision = DevNotebookStartupLogic.DetermineAction(
                settings.openOnLoad,
                settings.showGettingStartedOnLoad,
                settings.lastOpenedNotebookGuid,
                settings.RecentItems,
                notebookGuids);

            switch (decision.Action)
            {
                case DevNotebookStartupAction.OpenGettingStarted:
                    DevNotebookGettingStartedWindow.ShowWindow();
                    break;

                case DevNotebookStartupAction.OpenNotebookWindow:
                    DevNotebookWindow.ShowWindow(decision.NotebookGuid);
                    break;
            }
        }
    }
}
