# Security Policy

## Supported Versions

| Version        | Supported    |
| -------------- | ------------ |
| Latest beta    | ✅ Active     |
| Older releases | ❌ No patches |

## Reporting a Vulnerability

VaultWinnow handles sensitive Bitwarden/Vaultwarden vault data. 
Please **do not** open a public GitHub issue for security vulnerabilities.

Instead, use GitHub's private vulnerability reporting:
👉 [Report a vulnerability](https://github.com/cliftonfoster/vault-winnow/security/advisories/new)

I aim to acknowledge reports within **72 hours** and resolve confirmed 
vulnerabilities within **14 days** when possible.

## Security Design Notes

- VaultWinnow is **fully offline** — no network calls are made.
- It only reads/writes local JSON files or clipboard.
- No telemetry, analytics, or external connections of any kind.
- Only unencrypted Bitwarden JSON exports are accepted as input.