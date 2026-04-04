using System.Collections.Generic;
using System.Linq;
using VaultWinnow.Models;

namespace VaultWinnow // use the same namespace as VaultItem
{
    public static class DuplicateAnalyzer
    {
        public static void AnalyzeDuplicates(IEnumerable<VaultItem> items)
        {

            if (items == null)
            {
                return;
            }

            var itemList = items.ToList();
            if (itemList.Count == 0)
            {
                return;
            }

            // Reset previous analysis
            foreach (var item in itemList)
            {
                item.DuplicateStatus = DuplicateStatus.None;
                item.DuplicateGroupSize = 0;
                item.DuplicateGroupId = 0;   // or whatever your default is
                item.HasDuplicateAnalysis = false;
                item.DuplicateDiffCodes = null;
            }


            // Work only with login items that have a host/username/password
            var loginItems = itemList
                .Where(i => i.TypeLabel == "Login" && i.Login != null)
                .ToList();


            var groupKeyToId = new Dictionary<string, int>();
            int nextGroupId = 1;

            int GetGroupId(string key)
            {
                if (!groupKeyToId.TryGetValue(key, out var id))
                {
                    id = nextGroupId++;
                    groupKeyToId[key] = id;
                }
                return id;
            }

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
                    Name = i.Name ?? string.Empty,
                    Username = i.Username?.Trim().ToLowerInvariant() ?? string.Empty,
                    Password = i.Login?.Password ?? string.Empty,
                    Notes = i.Notes ?? string.Empty,
                    Totp = i.Login?.Totp ?? string.Empty,        // CHANGED: use value
                    HasPasskey = i.HasPasskey
                })
                .Where(g => g.Count() > 1)
                .ToList();



            // Mark strict duplicates
            foreach (var group in strictGroups)
            {
                int size = group.Count();

                // Build a key that ignores Name but captures host + username + password
                // so related Almost items can share the same ID.
                var any = group.First();
                string hostKey = GetHost(any.PrimaryUri);
                string userKey = (any.Username?.Trim().ToLowerInvariant() ?? string.Empty);
                string passKey = any.Login?.Password ?? string.Empty;

                string groupKey = $"{hostKey}|{userKey}|{passKey}";
                int groupId = GetGroupId(groupKey);

                foreach (var item in group)
                {
                    item.DuplicateStatus = DuplicateStatus.Strict;
                    item.DuplicateGroupSize = size;
                    item.DuplicateGroupId = groupId;
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
                        var totpValueA = a.Login?.Totp ?? string.Empty;
                        var totpValueB = b.Login?.Totp ?? string.Empty;

                        // Same username/password but 2FA/notes differ
                        bool sameCredsDifferentExtras =
                            sameUser &&
                            samePass &&
                            (
                                notesA != notesB ||
                                !string.Equals(totpValueA, totpValueB, StringComparison.Ordinal) ||
                                passkeyA != passkeyB ||
                                !string.Equals(a.Name, b.Name, StringComparison.Ordinal)
                            );

                        // Same host + same username, different password
                        bool sameUserDifferentPass = sameUser && passA != passB;

                        // Same host + same password, different username
                        bool samePassDifferentUser = samePass && userA != userB;

                        bool isAlmost = sameCredsDifferentExtras || sameUserDifferentPass || samePassDifferentUser;

                        if (!isAlmost)
                            continue;

                        // Compute diff codes once for this pair
                        var diffCodes = GetDiffCodes(a, b);

                        // Shared key for group ID (same as above: host + username + password)
                        var hostKey = GetHost(a.PrimaryUri);
                        var userKey = userA; // already normalized
                        var passKey = passA; // already captured above

                        string groupKey = $"{hostKey}|{userKey}|{passKey}";
                        int groupId = GetGroupId(groupKey);

                        void MergeDiffCodes(VaultItem item, string codes)
                        {
                            if (string.IsNullOrWhiteSpace(codes))
                                return;

                            if (string.IsNullOrWhiteSpace(item.DuplicateDiffCodes))
                            {
                                item.DuplicateDiffCodes = codes;
                                return;
                            }

                            var merged = item.DuplicateDiffCodes
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Concat(codes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                .Distinct()
                                .ToArray();

                            item.DuplicateDiffCodes = string.Join(",", merged);
                        }

                        void MarkAlmost(VaultItem item)
                        {
                            if (item.DuplicateStatus == DuplicateStatus.None)
                            {
                                item.DuplicateStatus = DuplicateStatus.Almost;
                                item.DuplicateGroupSize = Math.Max(item.DuplicateGroupSize, 2);
                            }

                            if (item.DuplicateGroupId == 0)
                                item.DuplicateGroupId = groupId;

                            MergeDiffCodes(item, diffCodes);
                        }

                        MarkAlmost(a);
                        MarkAlmost(b);

                    }
                }
            }

            foreach (var item in itemList)
            {
                item.HasDuplicateAnalysis = true;
            }

        }

        private static string GetDiffCodes(VaultItem a, VaultItem b)
        {
            if (a == null || b == null)
                return string.Empty;

            var codes = new List<string>();

            if (!string.Equals(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.Ordinal))
                codes.Add("N"); // Name

            if (!string.Equals(a.Username?.Trim() ?? string.Empty, b.Username?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                codes.Add("U"); // Username

            if (!string.Equals(a.Login?.Password ?? string.Empty, b.Login?.Password ?? string.Empty, StringComparison.Ordinal))
                codes.Add("P"); // Password

            if (!string.Equals(a.Notes ?? string.Empty, b.Notes ?? string.Empty, StringComparison.Ordinal))
                codes.Add("O"); // Notes

            bool aHasTotp = !string.IsNullOrWhiteSpace(a.Login?.Totp);
            bool bHasTotp = !string.IsNullOrWhiteSpace(b.Login?.Totp);
            string aTotp = a.Login?.Totp?.Trim() ?? string.Empty;
            string bTotp = b.Login?.Totp?.Trim() ?? string.Empty;

            if (aHasTotp != bHasTotp || !string.Equals(aTotp, bTotp, StringComparison.Ordinal))
                codes.Add("T"); // TOTP

            if (a.HasPasskey != b.HasPasskey)
                codes.Add("K"); // Passkey / FIDO2

            return string.Join(",", codes);
        }

        public static string GetDiffCodeDescription(string? codes)
        {
            if (string.IsNullOrWhiteSpace(codes))
                return string.Empty;

            var parts = codes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToUpperInvariant());

            var descriptions = new List<string>();

            foreach (var code in parts)
            {
                switch (code)
                {
                    case "N":
                        descriptions.Add("Name");
                        break;
                    case "U":
                        descriptions.Add("Username");
                        break;
                    case "P":
                        descriptions.Add("Password");
                        break;
                    case "O":
                        descriptions.Add("Notes");
                        break;
                    case "T":
                        descriptions.Add("TOTP");
                        break;
                    case "K":
                        descriptions.Add("Passkey");
                        break;
                }
            }

            return string.Join(", ", descriptions.Distinct());
        }
    }
}
