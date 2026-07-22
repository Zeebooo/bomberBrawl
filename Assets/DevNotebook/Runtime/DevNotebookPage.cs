using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevNotebook
{
    [Serializable]
    public class DevNotebookPage
    {
        public string title = "New Page";

        [TextArea(5, 20)]
        public string body = string.Empty;

        public bool isPinned;
        public DevNotebookPageStatus status = DevNotebookPageStatus.None;
        public List<string> tags = new List<string>();
        public List<DevNotebookChecklistItem> checklistItems = new List<DevNotebookChecklistItem>();
        public DevNotebookSceneLink linkedScene = new DevNotebookSceneLink();
        public List<UnityEngine.Object> linkedAssets = new List<UnityEngine.Object>();
        public long createdTicksUtc = DateTime.UtcNow.Ticks;
        public long updatedTicksUtc = DateTime.UtcNow.Ticks;

        public void Touch()
        {
            if (createdTicksUtc == 0L)
            {
                createdTicksUtc = DateTime.UtcNow.Ticks;
            }

            updatedTicksUtc = DateTime.UtcNow.Ticks;
        }
    }
}
