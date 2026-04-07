using System.Globalization;
using System.Windows.Data;
using VaultWinnow.Models;

namespace VaultWinnow
{
    public class SelectedRowDiffMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return string.Empty;

            if (values[0] is not VaultItem rowItem)
                return string.Empty;

            if (values[1] is not VaultItem selectedItem)
                return string.Empty;

            if (rowItem.DuplicateGroupId <= 0 || selectedItem.DuplicateGroupId <= 0)
                return string.Empty;

            if (rowItem.DuplicateGroupId != selectedItem.DuplicateGroupId)
                return string.Empty;

            return GetDiffCodes(selectedItem, rowItem);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
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

            var baselinePasskey = baseline.HasPasskey;
            var otherPasskey = other.HasPasskey;

            char n = string.Equals(baselineName, otherName, StringComparison.Ordinal) ? '-' : 'N';
            char u = string.Equals(baselineUser, otherUser, StringComparison.Ordinal) ? '-' : 'U';
            char p = string.Equals(baselinePassword, otherPassword, StringComparison.Ordinal) ? '-' : 'P';
            char o = string.Equals(baselineNotes, otherNotes, StringComparison.Ordinal) ? '-' : 'O';
            char t = baselineTotp == otherTotp ? '-' : 'T';
            char k = baselinePasskey == otherPasskey ? '-' : 'K';

            return new string(new[] { n, u, p, o, t, k });
        }
    }
}