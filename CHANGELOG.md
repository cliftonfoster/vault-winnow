# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added
- Added a new `VaultWinnow.Tests` xUnit project with initial unit tests for Strict and Almost duplicate detection on Login items.

### Changed
- Extracted duplicate analysis logic into a dedicated `DuplicateAnalyzer` helper class for easier maintenance and testing.
- Duplicate analysis now has automated tests for core scenarios.

### Fixed
- 

## 0.3.0-beta - 2026-03-22

### Added
- Duplicate analysis for Login items with three categories: Strict, Almost, and None.
- Duplicate, group size, and group ID columns to surface analysis results in the main grid.
- Analyze button to run duplicate analysis on the currently loaded vault.
- Select Strict action that selects only strict duplicates in the visible view, keeping one item per group unselected.
- Show only duplicates filter that narrows the grid to Strict and Almost items, combined with existing search and type filters.
- Invert selection action that flips checkboxes for visible items, useful for exporting non-duplicates after selecting Strict.
- Per-item flags and display logic so the Duplicate column is blank before analysis, then shows Strict/Almost/None afterward.

### Changed
- Strict duplicates now require matching name, host, username, password, notes, TOTP presence/value, and passkey presence.
- Almost duplicates now include items that share host and credentials but differ in name, notes, TOTP, or passkeys, and are never auto-selected.
- Duplicate items are grouped by a stable group ID derived from host + username + password so related Strict and Almost entries appear together in the grid.
- Select and Clear helpers continue to operate only on the currently visible, filtered items. 

### Fixed
- 

## [0.2.0-beta] - 2026-03-14

### Added
- Application icon and logo for VaultWinnow window and executable.
- Type filters to show only Logins, Secure Notes, Cards, or Identities.
- Status bar counts that reflect both search and type filters.
- Tooltip on the status bar file path so the full loaded file path is visible on hover, even when the text is truncated.
- Toolbar icon+text buttons with 24×24 PNG icons and descriptive tooltips for actions like Open, Export, Copy, Append, Select, Clear, and About.
- Keyboard shortcuts for common actions: Ctrl+O (Open), Ctrl+E (Export Selected), Ctrl+C (Copy JSON), Ctrl+A (Select All visible), and Ctrl+L (Clear Selection for visible items).

### Changed
- Select All and Clear Selection now affect only the currently visible (filtered) items instead of all loaded items.

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