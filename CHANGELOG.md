# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Application icon and logo for VaultWinnow window and executable.
- Type filters to show only Logins, Secure Notes, Cards, or Identities.

### Changed
- 

### Fixed
- 

## [0.1.0-beta] - 2026-03-11

### Added
- Initial public release of VaultWinnow.
- Load unencrypted Bitwarden/Vaultwarden JSON exports into a searchable grid.
- Display item type, name, username, primary URI, folder name, and indicators for TOTP and FIDO2/passkeys.
- Select items via checkboxes, with Select All and Clear Selection helpers.
- Export selected items to a new JSON file that includes only the used folders (Bitwarden/Vaultwarden import compatible).
- Copy the same filtered JSON to the clipboard for Bitwarden “paste JSON” import.
- Append selected items into an existing filtered JSON file, merging folders by ID.
- About dialog with app description, GitHub link, and Ko-fi support link.
- MIT license and project documentation (README and project overview).