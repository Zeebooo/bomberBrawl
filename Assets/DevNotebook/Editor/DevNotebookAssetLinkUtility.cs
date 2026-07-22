using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevNotebook.Editor
{
    internal static class DevNotebookAssetLinkUtility
    {
        public static int AddSelectionToPage(DevNotebookPage page, IEnumerable<Object> selection)
        {
            if (page == null || selection == null)
            {
                return 0;
            }

            page.linkedAssets ??= new List<Object>();

            int added = 0;
            foreach (Object candidate in selection)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (candidate is SceneAsset || !EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (page.linkedAssets.Contains(candidate))
                {
                    continue;
                }

                page.linkedAssets.Add(candidate);
                added++;
            }

            return added;
        }
    }
}
