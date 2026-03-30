# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added
- xUnit coverage for selection helpers (Select All, Clear Selection, Invert Selection) to ensure they operate only on the currently visible, filtered items.
- Extracted grid filtering logic into an ItemsFilterHelper so search text, type filters, and duplicate visibility are handled in one place.

### Changed
- Added a Close toolbar button and Ctrl+W shortcut to close the current file, with a confirmation if items are selected.
- Disabled Select and Clear toolbar actions until a file is loaded, so the initial toolbar state matches available actions.
- Centralized “no file loaded” UI reset into a single helper method to keep Open/Close/failure paths consistent.
- Set the Options panel (type filters, duplicate toggle, column visibility) to start collapsed so the main grid is less visually busy by default.
- Extracted selection-related logic into a dedicated SelectionHelper to keep MainWindow.xaml.cs smaller and easier to maintain, while preserving the existing runtime behavior for Select Strict.
- Updated the type filter to correctly treat Logins, Secure Notes, Cards, and Identities as combinable flags, so checking multiple types only shows those selected item types.

### Fixed
- Standardized button click handler names (`BtnSomethingClick`) for consistency across the codebase.
- Clarified tooltips for duplicate-related and MFA/Passkey columns so Strict/Almost/None and group/count semantics are easier to understand.
- Fixed an issue where selecting multiple type filters (for example, Cards and Identities together) would incorrectly show all items instead of only the chosen types.

## 0.4.0-beta - 2026-03-27

### Added
- Added a new `VaultWinnow.Tests` xUnit project with initial unit tests for Strict and Almost duplicate detection on Login items.
- App and About dialog branding with the new VaultWinnow logo, including a larger logo and stacked version line in the About window.
- Faded VaultWinnow logo watermark centered in the main DataGrid when no items are loaded, automatically hidden once a vault is opened.
- Column visibility toggles for duplicate-related and MFA/Passkey columns, so users can hide Dup, Count, Group, MFA, and Passkey when not needed.
- Collapsible “Options” panel that groups type filters, Show only duplicates, and column visibility controls to reduce vertical clutter while keeping filters close to the grid.

### Changed
- Refactored duplicate detection into a dedicated `DuplicateAnalyzer` helper class, independent of the WPF UI.
- Added a `VaultWinnow.Tests` xUnit project with tests covering Strict, Almost, non-duplicates, and mixed groups for login items.
- Tightened duplicate rules so TOTP value differences and passkey differences are treated as Almost, never Strict.
- Added a “Dup Help” toolbar button and Duplicate Analysis Help window explaining Strict / Almost / None behavior.
- Minor layout tweaks to the About window to improve readability and visual hierarchy.
- Toolbar layout with grouped, color-coded icon+label buttons and increased button height for clearer, vertically stacked labels.
- Main grid column widths (Name, Username, Primary URI, and small diagnostic columns) to start at sensible sizes and remain readable without manual resizing on load.
- DataGrid configuration to better support horizontal scrolling and manual column resizing when users need more space for long URIs or names.

### Fixed
- Duplicate, Count, Group, MFA, and Passkey column headers no longer appear squashed on initial load; headers and tooltips are fully visible.

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