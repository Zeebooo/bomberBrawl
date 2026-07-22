using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevNotebook
{
    [Serializable]
    public class DevNotebookSection
    {
        public string title = "New Section";
        public Color color = new Color(0.37f, 0.52f, 0.84f, 1f);
        public bool isPinned;
        public List<DevNotebookPage> pages = new List<DevNotebookPage>();
    }
}
