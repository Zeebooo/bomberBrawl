using System.IO;
using UnityEditor;
using UnityEngine;

namespace DevNotebook.Editor
{
    internal static class DevNotebookCreationUtility
    {
        public static DevNotebookNotebook CreateNotebookInSelectedFolder(bool revealInProjectWindow)
        {
            string targetFolder = GetSelectedFolderPath();
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                targetFolder = DevNotebookEditorPaths.NotebookRoot;
            }
            else if (targetFolder.StartsWith(DevNotebookEditorPaths.AssetRoot))
            {
                targetFolder = DevNotebookEditorPaths.NotebookRoot;
            }

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                EnsureFolder(targetFolder);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetFolder, "DevNotebook.asset"));
            var notebook = ScriptableObject.CreateInstance<DevNotebookNotebook>();
            notebook.title = Path.GetFileNameWithoutExtension(assetPath);
            notebook.description = "Notes attached to scenes, assets, and review work.";
            notebook.EnsureData();
            AssetDatabase.CreateAsset(notebook, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (revealInProjectWindow)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = notebook;
            }

            return notebook;
        }

        private static string GetSelectedFolderPath()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(path))
                {
                    return path;
                }

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && AssetDatabase.IsValidFolder(directory))
                {
                    return directory.Replace("\\", "/");
                }
            }

            return string.Empty;
        }

        private static void EnsureFolder(string targetFolder)
        {
            string normalized = targetFolder.Replace("\\", "/");
            string[] segments = normalized.Split('/');
            if (segments.Length == 0)
            {
                return;
            }

            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
