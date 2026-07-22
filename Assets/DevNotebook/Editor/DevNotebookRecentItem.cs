using System;

namespace DevNotebook.Editor
{
    [Serializable]
    internal class DevNotebookRecentItem
    {
        public string notebookGuid;
        public string notebookTitle;
        public string pageTitle;
        public int sectionIndex = -1;
        public int pageIndex = -1;
        public long openedTicksUtc;
    }
}
