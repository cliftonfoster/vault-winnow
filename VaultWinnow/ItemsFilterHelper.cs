using System;
using System.Globalization;
using VaultWinnow.Models;
using static VaultWinnow.MainWindow;

namespace VaultWinnow
{
    internal static class ItemsFilterHelper
    {
        public static bool MatchesFilter(
            VaultItem item,
            string? searchText,
            ItemTypeFilter typeFilter,
            bool showOnlyDuplicates)
        {
            if (item == null)
            {
                return false;
            }

            // Type filter
            if (!MatchesTypeFilter(item, typeFilter))
            {
                return false;
            }

            // Show only duplicates flag
            if (showOnlyDuplicates && item.DuplicateStatus == DuplicateStatus.None)
            {
                return false;
            }

            // Search text
            if (!MatchesSearchText(item, searchText))
            {
                return false;
            }

            return true;
        }

        private static bool MatchesTypeFilter(VaultItem item, ItemTypeFilter typeFilter)
        {
            // All or None => no type restriction
            if (typeFilter == ItemTypeFilter.None || typeFilter == ItemTypeFilter.All)
                return true;

            var label = item.TypeLabel;

            bool matchesLogin = typeFilter.HasFlag(ItemTypeFilter.Login) &&
                                     string.Equals(label, "Login", StringComparison.OrdinalIgnoreCase);
            bool matchesSecureNote = typeFilter.HasFlag(ItemTypeFilter.SecureNote) &&
                                     string.Equals(label, "Secure Note", StringComparison.OrdinalIgnoreCase);
            bool matchesCard = typeFilter.HasFlag(ItemTypeFilter.Card) &&
                                     string.Equals(label, "Card", StringComparison.OrdinalIgnoreCase);
            bool matchesIdentity = typeFilter.HasFlag(ItemTypeFilter.Identity) &&
                                     string.Equals(label, "Identity", StringComparison.OrdinalIgnoreCase);

            return matchesLogin || matchesSecureNote || matchesCard || matchesIdentity;
        }

        private static bool MatchesSearchText(VaultItem item, string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            var text = searchText.Trim();
            if (text.Length == 0)
            {
                return true;
            }

            var comparison = StringComparison.OrdinalIgnoreCase;

            return (item.Name?.IndexOf(text, comparison) ?? -1) >= 0
                || (item.Username?.IndexOf(text, comparison) ?? -1) >= 0
                || (item.PrimaryUri?.IndexOf(text, comparison) ?? -1) >= 0
                || (item.FolderName?.IndexOf(text, comparison) ?? -1) >= 0;
        }
    }
}
