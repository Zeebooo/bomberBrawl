using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DevNotebook.Editor
{
    internal static class DevNotebookSceneLinkUtility
    {
        public static void AssignScene(DevNotebookSceneLink link, SceneAsset sceneAsset)
        {
            if (link == null)
            {
                return;
            }

            if (sceneAsset == null)
            {
                link.Clear();
                return;
            }

            string path = AssetDatabase.GetAssetPath(sceneAsset);
            link.sceneGuid = AssetDatabase.AssetPathToGUID(path);
            link.scenePath = path;
            link.sceneName = sceneAsset.name;
        }

        public static SceneAsset ResolveSceneAsset(DevNotebookSceneLink link)
        {
            if (link == null)
            {
                return null;
            }

            string path = ResolveScenePath(link);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        public static string ResolveScenePath(DevNotebookSceneLink link)
        {
            if (link == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(link.sceneGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(link.sceneGuid);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    link.scenePath = path;
                    if (string.IsNullOrWhiteSpace(link.sceneName))
                    {
                        link.sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                    }

                    return path;
                }
            }

            return link.scenePath;
        }

        public static bool OpenLinkedScene(DevNotebookSceneLink link)
        {
            string path = ResolveScenePath(link);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() &&
                EditorSceneManager.OpenScene(path).IsValid();
        }

        public static void AssignCurrentScene(DevNotebookSceneLink link)
        {
            string currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            SceneAsset sceneAsset = string.IsNullOrWhiteSpace(currentScenePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScenePath);

            AssignScene(link, sceneAsset);
        }
    }
}
