using UnityEditor;

namespace DevNotebook.Editor
{
    internal static class DevNotebookDocumentationUtility
    {
        public static void OpenReadme()
        {
            var readme = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DevNotebookEditorPaths.ReadmePath);
            if (readme != null)
            {
                AssetDatabase.OpenAsset(readme);
            }
        }
    }
}
