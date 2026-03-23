using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using  VaultWinnow.Models;
using Formatting = Newtonsoft.Json.Formatting;

namespace VaultWinnow
{
    public partial class MainWindow : Window
    {
        private VaultExport? _loadedExport;
        private ObservableCollection<VaultItem> _items = new();
        private ICollectionView? _itemsView;
        private ItemTypeFilter _typeFilter = ItemTypeFilter.All;
        public ICommand OpenCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        private bool _showOnlyDuplicates;



        [Flags]
        public enum ItemTypeFilter
        {
            None = 0,
            Login = 1,
            SecureNote = 2,
            Card = 4,
            Identity = 8,
            All = Login | SecureNote | Card | Identity
        }

        public MainWindow()
        {
            InitializeComponent();

            ItemGrid.ItemsSource = _items;
            _itemsView = CollectionViewSource.GetDefaultView(_items);
            if (_itemsView != null)
                _itemsView.Filter = ItemsFilter;

            // Commands for keyboard shortcuts
            OpenCommand = new RelayCommand(_ => BtnOpen_Click(this, new RoutedEventArgs()),
                                           _ => BtnOpen.IsEnabled);
            ExportCommand = new RelayCommand(_ => BtnExport_Click(this, new RoutedEventArgs()),
                                             _ => BtnExport.IsEnabled);
            CopyCommand = new RelayCommand(_ => BtnCopyToClipboard_Click(this, new RoutedEventArgs()),
                                           _ => BtnCopyToClipboard.IsEnabled);
            SelectAllCommand = new RelayCommand(_ => BtnSelectAll_Click(this, new RoutedEventArgs()),
                                                _ => BtnSelectAll.IsEnabled);
            ClearSelectionCommand = new RelayCommand(_ => BtnClearSelection_Click(this, new RoutedEventArgs()),
                                                     _ => BtnClearSelection.IsEnabled);

            DataContext = this;

            if (TxtStatus != null)
            {
                TxtStatus.Text = "No file loaded.";
                TxtStatus.ToolTip = null;
                TxtStatus.Foreground = Brushes.Gray;
            }

            UpdateCount();
        }


        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open Vaultwarden / Bitwarden JSON Export",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            var json = File.ReadAllText(dlg.FileName);
            _loadedExport = JsonConvert.DeserializeObject<VaultExport>(json);

            if (_loadedExport == null)
            {
                MessageBox.Show("Failed to parse the JSON file.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _items.Clear();

            // Build a quick lookup for folder names
            var folderLookup = _loadedExport.Folders
                .Where(f => f.Id != null)
                .ToDictionary(f => f.Id!, f => f.Name ?? string.Empty);

            foreach (var item in _loadedExport.Items)
            {
                if (item.FolderId != null && folderLookup.TryGetValue(item.FolderId, out var folderName))
                    item.FolderName = folderName;
                else
                    item.FolderName = string.Empty;

                _items.Add(item);
            }


            _items.Clear();
            foreach (var item in _loadedExport.Items)
                _items.Add(item);
            if (TxtStatus != null)
            {
                TxtStatus.Text = $"Loaded: {dlg.FileName}";
                TxtStatus.ToolTip = dlg.FileName;
                TxtStatus.Foreground = Brushes.DarkGreen;
            }
            BtnAnalyzeDuplicates.IsEnabled = true;
            BtnSelectStrictDuplicates.IsEnabled = true;
            BtnExport.IsEnabled = true;
            BtnCopyToClipboard.IsEnabled = true;
            BtnAppendToJson.IsEnabled = true;
            BtnSelectAll.IsEnabled = true;
            BtnClearSelection.IsEnabled = true;


            if (TxtStatus != null)
            {
                string fullPath = dlg.FileName;
                string fileName = System.IO.Path.GetFileName(fullPath);

                // Example: Loaded: vault.json
                TxtStatus.Text = $"Loaded: {fileName}";
                TxtStatus.ToolTip = fullPath;   // full path on hover
                TxtStatus.Foreground = Brushes.Green;
            }

            UpdateCount();

            // Update footer count whenever IsSelected changes on any item
            foreach (var item in _items)
                item.PropertyChanged += (s, args) => UpdateCount();

            _itemsView = CollectionViewSource.GetDefaultView(_items);
            if (_itemsView != null)
            {
                _itemsView.Filter = ItemsFilter;
                _itemsView.Refresh();
            }

        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var outputJson = BuildFilteredExportJson();
            if (outputJson == null) return;

            var dlg = new SaveFileDialog
            {
                Title = "Save Filtered Vault Export",
                Filter = "JSON Files (*.json)|*.json",
                FileName = "vault_filtered_export.json"
            };

            if (dlg.ShowDialog() != true) return;

            File.WriteAllText(dlg.FileName, outputJson);

            MessageBox.Show(
                "Export complete.",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateCount()
        {
            int total = _items?.Count ?? 0;

            int visible = total;
            if (_itemsView != null)
            {
                visible = _itemsView.Cast<object>().Count();
            }

            int selected = _items?.Count(i => i.IsSelected) ?? 0;

            if (TxtCount == null)
                return; // XAML not initialized yet or window is closing

            TxtCount.Text = $"{selected} selected  {visible} visible of {total} total";
        }



        private string? BuildFilteredExportJson()
        {
            var export = BuildFilteredExport();
            if (export == null) return null;

            return JsonConvert.SerializeObject(
                export,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private void BtnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var outputJson = BuildFilteredExportJson();
            if (outputJson == null) return;

            Clipboard.SetText(outputJson); // standard WPF clipboard API [web:99]

            MessageBox.Show(
                "Filtered JSON has been copied to the clipboard.\n" +
                "You can now paste it into Bitwarden's import box.",
                "Copied to Clipboard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private VaultExport? BuildFilteredExport()
        {
            if (_loadedExport == null) return null;

            var selectedItems = _items.Where(i => i.IsSelected).ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("No items selected. Please check at least one item.",
                    "Nothing Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var usedFolderIds = selectedItems
                .Where(i => i.FolderId != null)
                .Select(i => i.FolderId)
                .Distinct()
                .ToHashSet();

            return new VaultExport
            {
                Encrypted = false,
                Folders = _loadedExport.Folders
                    .Where(f => f.Id != null && usedFolderIds.Contains(f.Id))
                    .ToList(),
                Items = selectedItems
            };
        }

        private void BtnAppendToJson_Click(object sender, RoutedEventArgs e)
        {
            var toAppend = BuildFilteredExport();
            if (toAppend == null) return;

            // 1. Pick the existing filtered JSON (e.g., the 25-item file)
            var openDlg = new OpenFileDialog
            {
                Title = "Select existing vault JSON to append to",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (openDlg.ShowDialog() != true) return;

            VaultExport? baseExport;

            try
            {
                var baseJson = File.ReadAllText(openDlg.FileName);
                baseExport = JsonConvert.DeserializeObject<VaultExport>(baseJson);
            }
            catch
            {
                baseExport = null;
            }

            if (baseExport == null)
            {
                MessageBox.Show("Failed to read or parse the existing JSON file.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. SAFETY CONFIRM GOES HERE
            var originalCount = baseExport.Items.Count;
            var appendCount = toAppend.Items.Count;

            var result = MessageBox.Show(
                $"You are about to append {appendCount} item(s) to a file\n" +
                $"that currently has {originalCount} item(s).\n\n" +
                "Continue?",
                "Confirm Append",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // 3. Append items (no dedupe by design)
            baseExport.Items.AddRange(toAppend.Items);

            // 4. Merge folders by id to avoid duplicate folder objects
            var folderById = baseExport.Folders
                .Where(f => f.Id != null)
                .ToDictionary(f => f.Id!, f => f);

            foreach (var f in toAppend.Folders)
            {
                if (f.Id != null && !folderById.ContainsKey(f.Id))
                {
                    folderById[f.Id] = f;
                }
            }

            baseExport.Folders = folderById.Values.ToList();

            // 5. Prompt for a NEW file name
            var saveDlg = new SaveFileDialog
            {
                Title = "Save appended vault JSON",
                Filter = "JSON Files (*.json)|*.json",
                FileName = Path.GetFileNameWithoutExtension(openDlg.FileName) + "_appended.json"
            };

            if (saveDlg.ShowDialog() != true) return;

            var outputJson = JsonConvert.SerializeObject(
                baseExport,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(saveDlg.FileName, outputJson);

            MessageBox.Show(
                "Items appended successfully.\n\n" +
                $"New file: {saveDlg.FileName}",
                "Append Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        private bool ItemsFilter(object obj)
        {
            if (obj is not VaultItem item)
                return false;

            // Text search (your existing logic)
            var text = TxtSearch?.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();

                var matchesText =
                    (item.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Username?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.PrimaryUri?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.FolderName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);

                if (!matchesText)
                    return false;
            }

            // Type filter (new logic)
            // If you haven't added the enum/field yet, see below.
            string type = item.TypeLabel;

            if (type == "Login" && !_typeFilter.HasFlag(ItemTypeFilter.Login))
                return false;

            if (type == "Secure Note" && !_typeFilter.HasFlag(ItemTypeFilter.SecureNote))
                return false;

            if (type == "Card" && !_typeFilter.HasFlag(ItemTypeFilter.Card))
                return false;

            if (type == "Identity" && !_typeFilter.HasFlag(ItemTypeFilter.Identity))
                return false;

            if (_showOnlyDuplicates && item.DuplicateStatus == DuplicateStatus.None)
                return false;

            return true;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _itemsView?.Refresh();

            UpdateCount();
        }
        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_itemsView == null)
                return;

            foreach (var obj in _itemsView)
            {
                if (obj is VaultItem item)
                    item.IsSelected = true;
            }

            UpdateCount();
        }

        private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_itemsView == null)
                return;

            foreach (var obj in _itemsView)
            {
                if (obj is VaultItem item)
                    item.IsSelected = false;
            }

            UpdateCount();
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
        }


        private void UpdateTypeFilterFromCheckboxes()
        {
            ItemTypeFilter filter = ItemTypeFilter.None;

            if (ChkLogin?.IsChecked == true)
                filter |= ItemTypeFilter.Login;
            if (ChkSecureNote?.IsChecked == true)
                filter |= ItemTypeFilter.SecureNote;
            if (ChkCard?.IsChecked == true)
                filter |= ItemTypeFilter.Card;
            if (ChkIdentity?.IsChecked == true)
                filter |= ItemTypeFilter.Identity;

            _typeFilter = filter == ItemTypeFilter.None ? ItemTypeFilter.All : filter;

            _itemsView?.Refresh();
            UpdateCount();
        }

        private void TypeFilterCheckboxChanged(object sender, RoutedEventArgs e)
        {
            UpdateTypeFilterFromCheckboxes();
        }

        public class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }

        private void BtnAnalyzeDuplicatesClick(object sender, RoutedEventArgs e)
        {
            if (_items == null || _items.Count == 0)
            {
                MessageBox.Show("No items loaded.", "Analyze duplicates", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Reset previous analysis  
            foreach (var item in _items)
            {
                item.DuplicateStatus = DuplicateStatus.None;
                item.DuplicateGroupSize = 0;
            }

            // Work only with login items that have a host/username/password
            var loginItems = _items
                .Where(i => i.TypeLabel == "Login" && i.Login != null)
                .ToList();

            // Helper: normalize host from PrimaryUri
            string GetHost(string? uri)
            {
                if (string.IsNullOrWhiteSpace(uri))
                    return string.Empty;

                if (!Uri.TryCreate(uri, UriKind.Absolute, out var u))
                    return uri.Trim();

                return u.Host.ToLowerInvariant();
            }

            // Build strict groups: host + username + password + notes + TOTP + FIDO2 presence
            var strictGroups = loginItems
                .GroupBy(i => new
                {
                    Host = GetHost(i.PrimaryUri),
                    Name = i.Name ?? string.Empty,  // NEW: include Name
                    Username = i.Username?.Trim().ToLowerInvariant() ?? string.Empty,
                    Password = i.Login?.Password ?? string.Empty,
                    Notes = i.Notes ?? string.Empty,
                    HasTotp = !string.IsNullOrWhiteSpace(i.Login?.Totp),
                    HasPasskey = i.HasPasskey
                })
                .Where(g => g.Count() > 1)
                .ToList();


            // Mark strict duplicates
            foreach (var group in strictGroups)
            {
                int size = group.Count();
                foreach (var item in group)
                {
                    item.DuplicateStatus = DuplicateStatus.Strict;
                    item.DuplicateGroupSize = size;
                }
            }

            // Almost duplicates:
            // Same host; not strict; at least one of:
            // - same username & password but TOTP/notes/FIDO2 differ
            // - same username, different password
            // - same password, different username
            var byHost = loginItems
                .GroupBy(i => GetHost(i.PrimaryUri))
                .Where(g => !string.IsNullOrEmpty(g.Key));

            foreach (var hostGroup in byHost)
            {
                var list = hostGroup.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var a = list[i];
                        var b = list[j];

                        // Skip pairs already marked strict
                        if (a.DuplicateStatus == DuplicateStatus.Strict && b.DuplicateStatus == DuplicateStatus.Strict)
                            continue;

                        var userA = a.Username?.Trim().ToLowerInvariant() ?? string.Empty;
                        var userB = b.Username?.Trim().ToLowerInvariant() ?? string.Empty;
                        var passA = a.Login?.Password ?? string.Empty;
                        var passB = b.Login?.Password ?? string.Empty;
                        var notesA = a.Notes ?? string.Empty;
                        var notesB = b.Notes ?? string.Empty;
                        bool totpA = !string.IsNullOrWhiteSpace(a.Login?.Totp);
                        bool totpB = !string.IsNullOrWhiteSpace(b.Login?.Totp);
                        bool passkeyA = a.HasPasskey;
                        bool passkeyB = b.HasPasskey;

                        bool sameUser = userA == userB;
                        bool samePass = passA == passB;

                        // Same username/password but 2FA/notes differ
                        bool sameCredsDifferentExtras =
                            sameUser &&
                            samePass &&
                            (notesA != notesB ||
                             totpA != totpB ||
                             passkeyA != passkeyB ||
                             !string.Equals(a.Name, b.Name, StringComparison.Ordinal));

                        // Same host + same username, different password
                        bool sameUserDifferentPass = sameUser && passA != passB;

                        // Same host + same password, different username
                        bool samePassDifferentUser = samePass && userA != userB;

                        bool isAlmost = sameCredsDifferentExtras || sameUserDifferentPass || samePassDifferentUser;

                        if (!isAlmost)
                            continue;

                        // Mark both as Almost if not already Strict
                        void MarkAlmost(VaultItem item)
                        {
                            if (item.DuplicateStatus == DuplicateStatus.None)
                            {
                                item.DuplicateStatus = DuplicateStatus.Almost;
                                item.DuplicateGroupSize = Math.Max(item.DuplicateGroupSize, 2);
                            }
                        }

                        MarkAlmost(a);
                        MarkAlmost(b);
                    }
                }
            }

            _itemsView?.Refresh();
            UpdateCount();

            MessageBox.Show(
                "Duplicate analysis complete.\n\n" +
                "Use the Duplicate and # columns, plus filters, to review results.",
                "Analyze duplicates",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnSelectStrictDuplicatesClick(object sender, RoutedEventArgs e)
        {
            if (_itemsView == null)
                return;

            // Group visible strict duplicates by the same strict key used in analysis
            string GetHost(string? uri)
            {
                if (string.IsNullOrWhiteSpace(uri))
                    return string.Empty;

                if (!Uri.TryCreate(uri, UriKind.Absolute, out var u))
                    return uri.Trim();

                return u.Host.ToLowerInvariant();
            }

            var visibleStrict = _itemsView
                .Cast<object>()
                .OfType<VaultItem>()
                .Where(i =>
                    i.DuplicateStatus == DuplicateStatus.Strict &&
                    i.TypeLabel == "Login" &&
                    i.Login != null)
                .GroupBy(i => new
                {
                    Host = GetHost(i.PrimaryUri),
                    Name = i.Name ?? string.Empty,  // NEW: include Name
                    Username = i.Username?.Trim().ToLowerInvariant() ?? string.Empty,
                    Password = i.Login?.Password ?? string.Empty,
                    Notes = i.Notes ?? string.Empty,
                    HasTotp = !string.IsNullOrWhiteSpace(i.Login?.Totp),
                    HasPasskey = i.HasPasskey
                });


            foreach (var group in visibleStrict)
            {
                // Choose one to keep unselected; here: the first in the group
                var itemsInGroup = group.ToList();
                if (itemsInGroup.Count <= 1)
                    continue;

                // Clear selection for all first
                foreach (var item in itemsInGroup)
                    item.IsSelected = false;

                // Keep the first unselected, select the rest as "safe duplicates"
                for (int i = 1; i < itemsInGroup.Count; i++)
                    itemsInGroup[i].IsSelected = true;
            }

            UpdateCount();
        }

        private void DuplicatesFilterChanged(object sender, RoutedEventArgs e)
        {
            _showOnlyDuplicates = ChkShowOnlyDuplicates?.IsChecked == true;

            if (_showOnlyDuplicates)
            {
                ApplyDuplicateSorting();
            }
            else
            {
                // Optional: clear duplicate-centric sorts; you can restore a default or leave as-is
                _itemsView?.SortDescriptions.Clear();
            }

            _itemsView?.Refresh();
            UpdateCount();
        }


        private void ApplyDuplicateSorting()
        {
            if (_itemsView == null)
                return;

            _itemsView.SortDescriptions.Clear();

            // 1. Duplicate status: Strict before Almost
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.DuplicateStatus), ListSortDirection.Ascending));

            // 2. Name (vault item name)
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.Name), ListSortDirection.Ascending));

            // 3. Username
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.Username), ListSortDirection.Ascending));

            // 4. Primary URI (host/path)
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.PrimaryUri), ListSortDirection.Ascending));

            // 5. Unselected listed first
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.IsSelected), ListSortDirection.Ascending));
        }




    }

}
