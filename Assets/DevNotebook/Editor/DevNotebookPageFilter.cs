using System;
using System.Linq;

namespace DevNotebook.Editor
{
    internal static class DevNotebookPageFilter
    {
        public static bool Matches(
            DevNotebookPage page,
            string searchText,
            DevNotebookPageStatus? statusFilter,
            string sceneGuidFilter)
        {
            if (page == null)
            {
                return false;
            }

            if (statusFilter.HasValue && page.status != statusFilter.Value)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(sceneGuidFilter) &&
                !string.Equals(page.linkedScene?.sceneGuid, sceneGuidFilter, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string needle = searchText.Trim();
            if (needle.Length == 0)
            {
                return true;
            }

            return Contains(page.title, needle) ||
                Contains(page.body, needle) ||
                (page.tags != null && page.tags.Any(tag => Contains(tag, needle)));
        }

        private static bool Contains(string haystack, string needle)
        {
            return !string.IsNullOrWhiteSpace(haystack) &&
                haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
