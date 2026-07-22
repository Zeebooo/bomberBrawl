using UnityEditor;

namespace DevNotebook.Editor
{
    internal static class DevNotebookMenuItems
    {
        [MenuItem("Window/DevNotebook/Notebook", priority = 0)]
        private static void OpenNotebookWindow()
        {
            DevNotebookWindow.ShowWindow();
        }

        [MenuItem("Window/DevNotebook/Getting Started", priority = 1)]
        private static void OpenGettingStarted()
        {
            DevNotebookGettingStartedWindow.ShowWindow();
        }

        [MenuItem("Assets/Create/DevNotebook/Notebook", priority = 2050)]
        private static void CreateNotebookAsset()
        {
            DevNotebookNotebook notebook = DevNotebookCreationUtility.CreateNotebookInSelectedFolder(true);
            if (notebook != null)
            {
                DevNotebookWindow.ShowWindow(DevNotebookNotebookRepository.GetGuid(notebook));
            }
        }
    }
}
