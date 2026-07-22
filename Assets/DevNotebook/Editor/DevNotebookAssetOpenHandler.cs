using UnityEditor;
using UnityEditor.Callbacks;

namespace DevNotebook.Editor
{
    internal static class DevNotebookAssetOpenHandler
    {
        [OnOpenAsset]
        private static bool OpenNotebookAsset(int instanceId, int line)
        {
            DevNotebookNotebook notebook = EditorUtility.InstanceIDToObject(instanceId) as DevNotebookNotebook;
            if (notebook == null)
            {
                return false;
            }

            string guid = DevNotebookNotebookRepository.GetGuid(notebook);
            DevNotebookWindow.ShowWindow(guid);
            return true;
        }
    }
}
