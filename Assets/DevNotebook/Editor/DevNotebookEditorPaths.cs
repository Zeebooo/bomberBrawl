using UnityEditor;
using UnityEngine.UIElements;

namespace DevNotebook.Editor
{
    internal static class DevNotebookEditorPaths
    {
        public const string AssetRoot = "Assets/DevNotebook";
        public const string NotebookRoot = "Assets/DevNotebooks";
        public const string SampleDefaultTargetFolder = NotebookRoot + "/DevNotebook Sample";
        public const string SettingsPath = "Project/DevNotebook";

        public static string ReadmePath => AssetRoot + "/README.md";
        public static string SampleSourceFolder => AssetRoot + "/Samples/Starter Content/Assets/DevNotebook Sample";

        public static StyleSheet LoadStyleSheet()
        {
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetRoot + "/Editor/Resources/Styles/DevNotebook.uss");
        }
    }
}
