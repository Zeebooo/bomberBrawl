using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevNotebook.Editor
{
    internal static class DevNotebookNotebookRepository
    {
        public static List<string> FindNotebookGuids()
        {
            return AssetDatabase.FindAssets("t:DevNotebookNotebook")
                .OrderBy(guid => AssetDatabase.GUIDToAssetPath(guid), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static DevNotebookNotebook LoadNotebook(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<DevNotebookNotebook>(path);
        }

        public static string GetGuid(DevNotebookNotebook notebook)
        {
            if (notebook == null)
            {
                return string.Empty;
            }

            string path = AssetDatabase.GetAssetPath(notebook);
            return string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        public static string GetDisplayName(string guid)
        {
            DevNotebookNotebook notebook = LoadNotebook(guid);
            if (notebook != null && !string.IsNullOrWhiteSpace(notebook.title))
            {
                return notebook.title;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path) ? "Missing Notebook" : Path.GetFileNameWithoutExtension(path);
        }

        public static string GetMostRecentExistingGuid(IReadOnlyList<DevNotebookRecentItem> recentItems)
        {
            if (recentItems == null)
            {
                return string.Empty;
            }

            foreach (DevNotebookRecentItem item in recentItems.OrderByDescending(entry => entry.openedTicksUtc))
            {
                if (item == null || string.IsNullOrWhiteSpace(item.notebookGuid))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(AssetDatabase.GUIDToAssetPath(item.notebookGuid)))
                {
                    return item.notebookGuid;
                }
            }

            return string.Empty;
        }
    }
}
