using UnityEditor;
using UnityEngine;

namespace DevNotebook.Editor
{
    internal static class DevNotebookSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(DevNotebookEditorPaths.SettingsPath, SettingsScope.Project)
            {
                label = "DevNotebook",
                guiHandler = DrawSettingsGui,
                keywords = new[] { "DevNotebook", "Notebook", "Open On Load", "Getting Started", "Recent" }
            };
        }

        private static void DrawSettingsGui(string searchContext)
        {
            DevNotebookProjectSettings settings = DevNotebookProjectSettings.instance;
            SerializedObject serializedObject = new SerializedObject(settings);
            serializedObject.Update();

            EditorGUILayout.LabelField("Startup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(DevNotebookProjectSettings.openOnLoad)), new GUIContent("Open On Load"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(DevNotebookProjectSettings.showGettingStartedOnLoad)), new GUIContent("Show Getting Started On Load"));
            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Recent Work", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(settings.recentItems.Count == 0))
            {
                if (GUILayout.Button("Clear Recent Items"))
                {
                    settings.ClearRecentItems();
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("Open DevNotebook"))
            {
                DevNotebookWindow.ShowWindow(settings.lastOpenedNotebookGuid);
            }

            if (GUILayout.Button("Open Getting Started"))
            {
                DevNotebookGettingStartedWindow.ShowWindow();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                settings.SaveSettings();
            }
        }
    }
}
