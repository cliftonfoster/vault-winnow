using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

            // Initial status bar state
            if (TxtStatus != null)
            {
                TxtStatus.Text = "No file loaded.";
                TxtStatus.ToolTip = null;
                TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
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

            return true;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _itemsView?.Refresh();

            UpdateCount();
        }
        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                item.IsSelected = true;

            UpdateCount();
        }

        private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                item.IsSelected = false;

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

    }
}
