using UnityEditor;

namespace DevNotebook.Editor
{
    internal static class DevNotebookSampleImporter
    {
        public static DevNotebookNotebook ImportAndOpenStarterNotebook()
        {
            string destination = AssetDatabase.GenerateUniqueAssetPath(DevNotebookEditorPaths.SampleDefaultTargetFolder);
            FileUtil.CopyFileOrDirectory(DevNotebookEditorPaths.SampleSourceFolder, destination);
            AssetDatabase.Refresh();

            string[] notebookGuids = AssetDatabase.FindAssets("t:DevNotebookNotebook", new[] { destination });
            if (notebookGuids == null || notebookGuids.Length == 0)
            {
                return null;
            }

            DevNotebookNotebook notebook = DevNotebookNotebookRepository.LoadNotebook(notebookGuids[0]);
            if (notebook != null)
            {
                Selection.activeObject = notebook;
                EditorGUIUtility.PingObject(notebook);
                DevNotebookWindow.ShowWindow(notebookGuids[0]);
            }

            return notebook;
        }
    }
}
