using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace DevNotebook.Editor
{
    [FilePath("ProjectSettings/DevNotebookSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class DevNotebookProjectSettings : ScriptableSingleton<DevNotebookProjectSettings>
    {
        private const int MaxRecentItems = 8;

        public bool openOnLoad = true;
        public bool showGettingStartedOnLoad = true;
        public string lastOpenedNotebookGuid;
        public List<DevNotebookRecentItem> recentItems = new List<DevNotebookRecentItem>();

        public IReadOnlyList<DevNotebookRecentItem> RecentItems => recentItems;

        public void SaveSettings()
        {
            Save(true);
        }

        public void RecordNotebookOpened(string notebookGuid, string notebookTitle)
        {
            lastOpenedNotebookGuid = notebookGuid;

            if (string.IsNullOrWhiteSpace(notebookGuid))
            {
                SaveSettings();
                return;
            }

            DevNotebookRecentItem existing = recentItems.FirstOrDefault(item =>
                item != null &&
                item.notebookGuid == notebookGuid &&
                item.sectionIndex < 0 &&
                item.pageIndex < 0);

            if (existing == null)
            {
                existing = new DevNotebookRecentItem
                {
                    notebookGuid = notebookGuid,
                    sectionIndex = -1,
                    pageIndex = -1
                };
                recentItems.Add(existing);
            }

            existing.notebookTitle = notebookTitle;
            existing.pageTitle = string.Empty;
            existing.openedTicksUtc = DevNotebookTimeUtility.NowTicksUtc();

            SortAndTrim();
            SaveSettings();
        }

        public void RecordPageOpened(string notebookGuid, string notebookTitle, int sectionIndex, int pageIndex, string pageTitle)
        {
            lastOpenedNotebookGuid = notebookGuid;

            if (string.IsNullOrWhiteSpace(notebookGuid))
            {
                SaveSettings();
                return;
            }

            DevNotebookRecentItem existing = recentItems.FirstOrDefault(item =>
                item != null &&
                item.notebookGuid == notebookGuid &&
                item.sectionIndex == sectionIndex &&
                item.pageIndex == pageIndex);

            if (existing == null)
            {
                existing = new DevNotebookRecentItem();
                recentItems.Add(existing);
            }

            existing.notebookGuid = notebookGuid;
            existing.notebookTitle = notebookTitle;
            existing.sectionIndex = sectionIndex;
            existing.pageIndex = pageIndex;
            existing.pageTitle = pageTitle;
            existing.openedTicksUtc = DevNotebookTimeUtility.NowTicksUtc();

            SortAndTrim();
            SaveSettings();
        }

        public void PruneInvalidEntries()
        {
            recentItems.RemoveAll(item => item == null || string.IsNullOrWhiteSpace(item.notebookGuid));

            for (int index = recentItems.Count - 1; index >= 0; index--)
            {
                DevNotebookRecentItem item = recentItems[index];
                string path = AssetDatabase.GUIDToAssetPath(item.notebookGuid);

                if (string.IsNullOrWhiteSpace(path))
                {
                    recentItems.RemoveAt(index);
                }
            }

            if (!string.IsNullOrWhiteSpace(lastOpenedNotebookGuid) &&
                string.IsNullOrWhiteSpace(AssetDatabase.GUIDToAssetPath(lastOpenedNotebookGuid)))
            {
                lastOpenedNotebookGuid = string.Empty;
            }

            SortAndTrim();
            SaveSettings();
        }

        public void ClearRecentItems()
        {
            recentItems.Clear();
            SaveSettings();
        }

        private void SortAndTrim()
        {
            recentItems = recentItems
                .OrderByDescending(item => item.openedTicksUtc)
                .Take(MaxRecentItems)
                .ToList();
        }
    }
}
