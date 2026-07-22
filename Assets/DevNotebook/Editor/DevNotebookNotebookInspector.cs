using UnityEditor;
using UnityEngine;

namespace DevNotebook.Editor
{
    [CustomEditor(typeof(DevNotebookNotebook))]
    internal sealed class DevNotebookNotebookInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);

            DevNotebookNotebook notebook = (DevNotebookNotebook)target;
            string guid = DevNotebookNotebookRepository.GetGuid(notebook);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open in DevNotebook", GUILayout.Height(28f)))
                {
                    DevNotebookWindow.ShowWindow(guid);
                }

                if (GUILayout.Button("Ping Asset", GUILayout.Height(28f)))
                {
                    EditorGUIUtility.PingObject(notebook);
                    Selection.activeObject = notebook;
                }
            }
        }
    }
}
