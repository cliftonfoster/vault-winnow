// Models/VaultModels.cs
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
// Only Newtonsoft is used for JSON; this alias keeps your existing JsonIgnore usage.
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace VaultWinnow.Models
{
    public class VaultExport
    {
        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }

        [JsonProperty("folders")]
        public List<VaultFolder> Folders { get; set; } = new();

        [JsonProperty("items")]
        public List<VaultItem> Items { get; set; } = new();
    }

    public class VaultFolder
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    public class VaultItem : INotifyPropertyChanged
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("organizationId")]
        public string? OrganizationId { get; set; }

        [JsonProperty("folderId")]
        public string? FolderId { get; set; }

        [JsonIgnore]
        public string TypeLabel => Type switch
        {
            1 => "Login",
            2 => "Secure Note",
            3 => "Card",
            4 => "Identity",
            _ => $"Type {Type}"
        };


        [JsonIgnore]
        public string? FolderName { get; set; }

        // 1 = Login, 2 = Secure Note, 3 = Card, 4 = Identity [web:59]
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        // Freeform text on the item (applies to all types)
        [JsonProperty("notes")]
        public string? Notes { get; set; }

        // Marked as favorite in Bitwarden UI
        [JsonProperty("favorite")]
        public bool Favorite { get; set; }

        // Master password re-prompt setting
        [JsonProperty("reprompt")]
        public int Reprompt { get; set; }

        // Per-item custom fields (Name/Value/Type) [web:67]
        [JsonProperty("fields")]
        public List<VaultCustomField>? Fields { get; set; }

        // Logins (type = 1)
        [JsonProperty("login")]
        public VaultLogin? Login { get; set; }

        // Secure notes (type = 2)
        [JsonProperty("secureNote")]
        public VaultSecureNote? SecureNote { get; set; }

        // Cards (type = 3)
        [JsonProperty("card")]
        public VaultCard? Card { get; set; }

        // Identities (type = 4)
        [JsonProperty("identity")]
        public VaultIdentity? Identity { get; set; }

        // --- UI-only helpers, excluded from JSON output ---

        [JsonIgnore]
        public DuplicateStatus DuplicateStatus { get; set; } = DuplicateStatus.None;

        [JsonIgnore]
        public int DuplicateGroupSize { get; set; } = 0;


        [JsonIgnore]
        private bool _isSelected;

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        // Convenience computed properties for the grid
        [JsonIgnore]
        public string? Username => Login?.Username;

        [JsonIgnore]
        public string? PrimaryUri =>
            Login?.Uris != null && Login.Uris.Count > 0
                ? Login.Uris[0].Uri
                : null;

        [JsonIgnore]
        public bool HasTotp => Login?.Totp is not null && Login.Totp.Length > 0;

        [JsonIgnore]
        public bool HasPasskey =>
            Login?.Fido2Credentials is { Count: > 0 };


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class VaultLogin
    {
        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("password")]
        public string? Password { get; set; }

        [JsonProperty("uris")]
        public List<VaultUri>? Uris { get; set; }

        // TOTP secret/URI (only present when 2FA is configured) [web:59]
        [JsonProperty("totp")]
        public string? Totp { get; set; }

        // Stored passkeys / WebAuthn credentials [web:62][web:68]
        [JsonProperty("fido2Credentials")]
        public List<VaultFido2Credential>? Fido2Credentials { get; set; }
    }

    public class VaultUri
    {
        [JsonProperty("match")]
        public string? Match { get; set; }

        [JsonProperty("uri")]
        public string? Uri { get; set; }
    }

    public class VaultCustomField
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        // 0 = Text, 1 = Hidden, 2 = Boolean, 3 = Linked [web:67]
        [JsonProperty("type")]
        public int Type { get; set; }
    }

    public class VaultSecureNote
    {
        // Bitwarden uses a small numeric type enum for secure notes
        [JsonProperty("type")]
        public int Type { get; set; }
    }

    public class VaultCard
    {
        [JsonProperty("cardholderName")]
        public string? CardholderName { get; set; }

        [JsonProperty("brand")]
        public string? Brand { get; set; }

        [JsonProperty("number")]
        public string? Number { get; set; }

        [JsonProperty("expMonth")]
        public string? ExpMonth { get; set; }

        [JsonProperty("expYear")]
        public string? ExpYear { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }
    }

    public class VaultIdentity
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("firstName")]
        public string? FirstName { get; set; }

        [JsonProperty("middleName")]
        public string? MiddleName { get; set; }

        [JsonProperty("lastName")]
        public string? LastName { get; set; }

        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("company")]
        public string? Company { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("phone")]
        public string? Phone { get; set; }

        [JsonProperty("address1")]
        public string? Address1 { get; set; }

        [JsonProperty("address2")]
        public string? Address2 { get; set; }

        [JsonProperty("address3")]
        public string? Address3 { get; set; }

        [JsonProperty("city")]
        public string? City { get; set; }

        [JsonProperty("state")]
        public string? State { get; set; }

        [JsonProperty("postalCode")]
        public string? PostalCode { get; set; }

        [JsonProperty("country")]
        public string? Country { get; set; }

        [JsonProperty("ssn")]
        public string? Ssn { get; set; }

        [JsonProperty("passportNumber")]
        public string? PassportNumber { get; set; }

        [JsonProperty("licenseNumber")]
        public string? LicenseNumber { get; set; }
    }

    public class VaultFido2Credential
    {
        [JsonProperty("credentialId")]
        public string? CredentialId { get; set; }

        [JsonProperty("keyType")]
        public string? KeyType { get; set; }

        [JsonProperty("keyAlgorithm")]
        public string? KeyAlgorithm { get; set; }

        [JsonProperty("keyCurve")]
        public string? KeyCurve { get; set; }

        [JsonProperty("keyValue")]
        public string? KeyValue { get; set; }

        [JsonProperty("rpId")]
        public string? RpId { get; set; }

        [JsonProperty("userHandle")]
        public string? UserHandle { get; set; }

        [JsonProperty("userName")]
        public string? UserName { get; set; }

        [JsonProperty("counter")]
        public string? Counter { get; set; }

        [JsonProperty("rpName")]
        public string? RpName { get; set; }

        [JsonProperty("userDisplayName")]
        public string? UserDisplayName { get; set; }

        [JsonProperty("discoverable")]
        public string? Discoverable { get; set; }

        [JsonProperty("creationDate")]
        public string? CreationDate { get; set; }
    }
        public enum DuplicateStatus
    {
        None,
        Strict,
        Almost
    }
}
