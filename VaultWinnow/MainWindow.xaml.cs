using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private bool _hasDuplicateAnalysis;
        public ICommand CloseCommand { get; }
        private bool _hasShownAnalyzeCompletedMessage;
        private VaultItem? _selectedComparisonItem;
        private int _selectedDuplicateGroupId;

        public ObservableCollection<VaultItem> Items
        {
            get => _items;
            set => _items = value;
        }

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

            CloseCommand = new RelayCommand(_ => BtnCloseClick(this, new RoutedEventArgs()),
                    _ => BtnClose.IsEnabled);
                    DataContext = this;
            Items = new ObservableCollection<VaultItem>();


            ItemGrid.ItemsSource = _items;
            _itemsView = CollectionViewSource.GetDefaultView(_items);
            if (_itemsView != null)
                _itemsView.Filter = ItemsFilter;

            // Commands for keyboard shortcuts
            OpenCommand = new RelayCommand(_ => BtnOpenClick(this, new RoutedEventArgs()),
                                           _ => BtnOpen.IsEnabled);
            ExportCommand = new RelayCommand(_ => BtnExportClick(this, new RoutedEventArgs()),
                                             _ => BtnExport.IsEnabled);
            CopyCommand = new RelayCommand(_ => BtnCopyToClipboardClick(this, new RoutedEventArgs()),
                                           _ => BtnCopyToClipboard.IsEnabled);
            SelectAllCommand = new RelayCommand(_ => BtnSelectAllClick(this, new RoutedEventArgs()),
                                                _ => BtnSelectAll.IsEnabled);
            ClearSelectionCommand = new RelayCommand(_ => BtnClearSelectionClick(this, new RoutedEventArgs()),
                                                     _ => BtnClearSelection.IsEnabled);

            DataContext = this;

            DataContext = this;

            ResetUiToNoFileState();
        }


        private void BtnOpenClick(object sender, RoutedEventArgs e)
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

                ResetUiToNoFileState();
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
            BtnClose.IsEnabled = true;
            BtnExport.IsEnabled = true;
            BtnCopyToClipboard.IsEnabled = true;
            BtnAppendToJson.IsEnabled = true;
            BtnSelectAll.IsEnabled = true;
            BtnClearSelection.IsEnabled = true;
            BtnInvertSelection.IsEnabled = true;
            _hasDuplicateAnalysis = false;
            BtnAnalyzeDuplicates.IsEnabled = true;
            BtnSelectStrictDuplicates.IsEnabled = false;
            ChkShowOnlyDuplicates.IsEnabled = false;
            ChkShowOnlyDuplicates.IsChecked = false;



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

            foreach (var item in _items)
            {
                item.DuplicateStatus = DuplicateStatus.None;
                item.DuplicateGroupSize = 0;
                item.DuplicateGroupId = 0;
                item.HasDuplicateAnalysis = false;
            }

            // auto-size main text columns after data is loaded
            ItemGrid.UpdateLayout();

            // 0: checkbox, 1: Name, 2: Username, 3: Primary URI
            int nameIndex = 1;
            int usernameIndex = 2;
            int uriIndex = 3;

            if (ItemGrid.Columns.Count > uriIndex)
            {
                ItemGrid.Columns[nameIndex].Width = DataGridLength.Auto;
                ItemGrid.Columns[usernameIndex].Width = DataGridLength.Auto;
                ItemGrid.Columns[uriIndex].Width = new DataGridLength(2, DataGridLengthUnitType.Star);
            }

        }

        private void BtnCloseClick(object sender, RoutedEventArgs e)
        {
            if (_items != null && _items.Any(i => i.IsSelected))
            {
                var result = MessageBox.Show(
                    "You have selected items. Close without exporting?",
                    "Close file",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            ResetUiToNoFileState();
        }

        private void BtnExportClick(object sender, RoutedEventArgs e)
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
            int selectedVisible = 0;

            if (_itemsView != null)
            {
                var visibleItems = _itemsView.Cast<object>().OfType<VaultItem>().ToList();
                visible = visibleItems.Count;
                selectedVisible = visibleItems.Count(i => i.IsSelected);
            }

            int selectedTotal = _items?.Count(i => i.IsSelected) ?? 0;
            int selectedHidden = selectedTotal - selectedVisible;

            if (TxtCount == null)
                return;

            string selectedDetail = string.Empty;

            // Only show the detail when there are hidden selected items
            if (selectedHidden > 0)
            {
                selectedDetail = $" ({selectedVisible} visible, {selectedHidden} hidden)";
            }

            TxtCount.Text =
                $"{selectedTotal} selected{selectedDetail}  {visible} visible of {total} total";
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

        private void BtnCopyToClipboardClick(object sender, RoutedEventArgs e)
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

        private void BtnAppendToJsonClick(object sender, RoutedEventArgs e)
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
            {
                return false;
            }

            return ItemsFilterHelper.MatchesFilter(
                item,
                TxtSearch?.Text,
                _typeFilter,
                ChkShowOnlyDuplicates?.IsChecked == true);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _itemsView?.Refresh();

            UpdateCount();
        }

        private void ItemGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedComparisonItem = ItemGrid.SelectedItem as VaultItem;
            _selectedDuplicateGroupId = _selectedComparisonItem?.DuplicateGroupId ?? 0;

            foreach (var item in _items.OfType<VaultItem>())
            {
                item.IsInSelectedDuplicateGroup =
                    _selectedDuplicateGroupId > 0 &&
                    item.DuplicateGroupId == _selectedDuplicateGroupId;

                item.SelectedDiffCodes = string.Empty;
            }

            if (_selectedComparisonItem is not null && _selectedDuplicateGroupId > 0)
            {
                foreach (var item in _items.OfType<VaultItem>()
                                          .Where(i => i.DuplicateGroupId == _selectedDuplicateGroupId))
                {
                    item.SelectedDiffCodes = GetDiffCodes(_selectedComparisonItem, item);
                }
            }

            _itemsView?.Refresh();
        }

        private void BtnSelectAllClick(object sender, RoutedEventArgs e)
        {
            SelectionHelper.SelectAll(_itemsView);
            UpdateCount();
        }

        private void BtnClearSelectionClick(object sender, RoutedEventArgs e)
        {
            SelectionHelper.ClearSelection(_itemsView);
            UpdateCount();
        }

        private void BtnInvertSelectionClick(object sender, RoutedEventArgs e)
        {
            SelectionHelper.InvertSelection(_itemsView);
            UpdateCount();
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
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
                MessageBox.Show("No items loaded.", "Analyze duplicates",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DuplicateAnalyzer.AnalyzeDuplicates(_items);

            _itemsView?.Refresh();
            UpdateCount();
            _hasDuplicateAnalysis = true;
            BtnSelectStrictDuplicates.IsEnabled = true;
            ChkShowOnlyDuplicates.IsEnabled = true;

if (!_hasShownAnalyzeCompletedMessage)
{
    MessageBox.Show(
        "Duplicate analysis complete.\n\nUse the Duplicate and group columns, plus filters, to review results.",
        "Analyze duplicates",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

    _hasShownAnalyzeCompletedMessage = true;
}
        }

        private void BtnDuplicateHelpClick(object sender, RoutedEventArgs e)
        {
            var window = new DuplicateHelpWindow
            {
                Owner = this
            };
            window.ShowDialog();
        }


        private void BtnSelectStrictDuplicatesClick(object sender, RoutedEventArgs e)
        {
            if (!_hasDuplicateAnalysis)
                return;

            SelectionHelper.SelectStrictDuplicatesInView(_itemsView);
            UpdateCount();
        }

        private void DuplicatesFilterChanged(object sender, RoutedEventArgs e)
        {
            if (!_hasDuplicateAnalysis) return;

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

            // 1. Strict before Almost
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.DuplicateStatus), ListSortDirection.Ascending));

            // 2. Group ID keeps related items adjacent
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.DuplicateGroupId), ListSortDirection.Ascending));

            // 3. Name
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.Name), ListSortDirection.Ascending));

            // 4. Primary URI
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.PrimaryUri), ListSortDirection.Ascending));

            // 5. Username
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.Username), ListSortDirection.Ascending));

            // 6. Unchecked first, then checked
            _itemsView.SortDescriptions.Add(
                new SortDescription(nameof(VaultItem.IsSelected), ListSortDirection.Ascending));
        }

        private static string GetDiffCodes(VaultItem? baseline, VaultItem? other)
        {
            if (baseline is null || other is null)
                return string.Empty;

            if (ReferenceEquals(baseline, other))
                return "------";

            static string Norm(string? value) => value?.Trim() ?? string.Empty;
            static string NormLower(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
            static bool HasTotp(VaultItem item) => !string.IsNullOrWhiteSpace(item.Login?.Totp);
            static bool HasPasskey(VaultItem item) => item.HasPasskey;

            var baselineName = Norm(baseline.Name);
            var otherName = Norm(other.Name);

            var baselineUser = NormLower(baseline.Username);
            var otherUser = NormLower(other.Username);

            var baselinePassword = baseline.Login?.Password ?? string.Empty;
            var otherPassword = other.Login?.Password ?? string.Empty;

            var baselineNotes = Norm(baseline.Notes);
            var otherNotes = Norm(other.Notes);

            var baselineTotp = HasTotp(baseline);
            var otherTotp = HasTotp(other);

            var baselinePasskey = HasPasskey(baseline);
            var otherPasskey = HasPasskey(other);

            char n = string.Equals(baselineName, otherName, StringComparison.Ordinal) ? '-' : 'N';
            char u = string.Equals(baselineUser, otherUser, StringComparison.Ordinal) ? '-' : 'U';
            char p = string.Equals(baselinePassword, otherPassword, StringComparison.Ordinal) ? '-' : 'P';
            char o = string.Equals(baselineNotes, otherNotes, StringComparison.Ordinal) ? '-' : 'O';
            char t = baselineTotp == otherTotp ? '-' : 'T';
            char k = baselinePasskey == otherPasskey ? '-' : 'K';

            return $"{n}{u}{p}{o}{t}{k}";
        }

        private void ColumnVisibilityChanged(object sender, RoutedEventArgs e)
        {
            if (ItemGrid == null)
                return;

            // Adjust indices if your column order changes
            // 0: checkbox, 1: Name, 2: Username, 3: Primary URI, 4: Type
            int dupIndex = 5;
            int dupCountIndex = 6;
            int dupGroupIndex = 7;
            int folderIndex = 8;
            int mfaIndex = 9;
            int passkeyIndex = 10;

            if (ItemGrid.Columns.Count > passkeyIndex)
            {
                ItemGrid.Columns[dupIndex].Visibility =
                    ChkColDup.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                ItemGrid.Columns[dupCountIndex].Visibility =
                    ChkColDupCount.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                ItemGrid.Columns[dupGroupIndex].Visibility =
                    ChkColDupGroup.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                ItemGrid.Columns[mfaIndex].Visibility =
                    ChkColMfa.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                ItemGrid.Columns[passkeyIndex].Visibility =
                    ChkColPasskey.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ResetUiToNoFileState()
        {
            // Clear data
            _items.Clear();

            if (_itemsView != null)
            {
                _itemsView.Refresh();
            }

            // Reset filters / search
            TxtSearch.Text = string.Empty;

            // Status text
            TxtStatus.Text = "No file loaded.";
            TxtStatus.ToolTip = null;

            // Disable file-dependent controls
            BtnExport.IsEnabled = false;
            BtnCopyToClipboard.IsEnabled = false;
            BtnAppendToJson.IsEnabled = false;
            BtnSelectAll.IsEnabled = false;
            BtnClearSelection.IsEnabled = false;
            BtnInvertSelection.IsEnabled = false;
            BtnAnalyzeDuplicates.IsEnabled = false;
            BtnSelectStrictDuplicates.IsEnabled = false;
            ChkShowOnlyDuplicates.IsEnabled = false;
            BtnClose.IsEnabled = false;

            // Reset counts/footer
            UpdateCount();
        }

    }

}
