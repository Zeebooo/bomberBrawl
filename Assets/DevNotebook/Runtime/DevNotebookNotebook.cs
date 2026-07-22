using System.Collections.Generic;
using UnityEngine;

namespace DevNotebook
{
    public class DevNotebookNotebook : ScriptableObject
    {
        public string title = "Dev Notebook";

        [TextArea(2, 5)]
        public string description = "Keep notes attached to your project scenes and assets.";

        public Color accentColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        public List<DevNotebookSection> sections = new List<DevNotebookSection>();

        public void EnsureData()
        {
            sections ??= new List<DevNotebookSection>();

            foreach (DevNotebookSection section in sections)
            {
                if (section == null)
                {
                    continue;
                }

                section.pages ??= new List<DevNotebookPage>();

                foreach (DevNotebookPage page in section.pages)
                {
                    if (page == null)
                    {
                        continue;
                    }

                    page.tags ??= new List<string>();
                    page.checklistItems ??= new List<DevNotebookChecklistItem>();
                    page.linkedAssets ??= new List<Object>();
                    page.linkedScene ??= new DevNotebookSceneLink();

                    if (page.createdTicksUtc == 0L)
                    {
                        page.Touch();
                    }
                }
            }
        }
    }
}
