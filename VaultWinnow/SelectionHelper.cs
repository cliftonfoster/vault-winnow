using System.ComponentModel;
using System.Windows.Data;
using VaultWinnow.Models;

namespace VaultWinnow
{
    public static class SelectionHelper
    {
        public static void SelectAll(ICollectionView? itemsView)
        {
            if (itemsView == null)
                return;

            foreach (var obj in itemsView)
            {
                if (obj is VaultItem item)
                    item.IsSelected = true;
            }
        }

        public static void ClearSelection(ICollectionView? itemsView)
        {
            if (itemsView == null)
                return;

            foreach (var obj in itemsView)
            {
                if (obj is VaultItem item)
                    item.IsSelected = false;
            }
        }

        public static void InvertSelection(ICollectionView? itemsView)
        {
            if (itemsView == null)
                return;

            foreach (var obj in itemsView)
            {
                if (obj is VaultItem item)
                    item.IsSelected = !item.IsSelected;
            }
        }

        public static void SelectStrictDuplicatesInView(ICollectionView? itemsView)
        {
            if (itemsView == null)
                return;

            // Local helper matches MainWindow's GetHost logic
            static string GetHost(string? uri)
            {
                if (string.IsNullOrWhiteSpace(uri))
                    return string.Empty;

                if (!Uri.TryCreate(uri, UriKind.Absolute, out var u))
                    return uri.Trim();

                return u.Host.ToLowerInvariant();
            }

            var visibleStrictGroups = itemsView
                .Cast<object>()
                .OfType<VaultItem>()
                .Where(i =>
                    i.DuplicateStatus == DuplicateStatus.Strict &&
                    i.TypeLabel == "Login" &&
                    i.Login != null)
                .GroupBy(i => new
                {
                    Host = GetHost(i.PrimaryUri),
                    Name = i.Name ?? string.Empty,
                    Username = i.Username?.Trim().ToLowerInvariant() ?? string.Empty,
                    Password = i.Login?.Password ?? string.Empty,
                    Notes = i.Notes ?? string.Empty,
                    HasTotp = !string.IsNullOrWhiteSpace(i.Login?.Totp),
                    HasPasskey = i.HasPasskey
                });

            foreach (var group in visibleStrictGroups)
            {
                var itemsInGroup = group.ToList();
                if (itemsInGroup.Count <= 1)
                    continue;

                // Clear selection for all in the group
                foreach (var item in itemsInGroup)
                    item.IsSelected = false;

                // Keep the first unselected, select the rest
                for (int i = 1; i < itemsInGroup.Count; i++)
                    itemsInGroup[i].IsSelected = true;
            }
        }
    }
}