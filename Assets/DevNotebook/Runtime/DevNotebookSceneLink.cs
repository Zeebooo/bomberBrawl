using System;

namespace DevNotebook
{
    [Serializable]
    public class DevNotebookSceneLink
    {
        public string sceneGuid;
        public string scenePath;
        public string sceneName;

        public bool HasValue =>
            !string.IsNullOrWhiteSpace(sceneGuid) ||
            !string.IsNullOrWhiteSpace(scenePath) ||
            !string.IsNullOrWhiteSpace(sceneName);

        public void Clear()
        {
            sceneGuid = string.Empty;
            scenePath = string.Empty;
            sceneName = string.Empty;
        }
    }
}
