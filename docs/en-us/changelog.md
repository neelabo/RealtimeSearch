# Change Log

## 4.1
(2025-03-25)

### Fixed

- Fixed a slight GPU load when doing nothing.

## 4.0
(2025-01-12)

> [!IMPORTANT]
> Operating environment is Windows 10  (64bit) or later.  

> [!IMPORTANT]
> Configuration data is not compatible with previous versions. Please make new settings.

### Added

- Store app now available.
- Languages support (English and Japanese).
- Indexes are cached so that searches can be performed immediately after the next startup.

### Changed

- Updated environment to .NET 9.
- Revamped UI of the settings window.
- Added ON/OFF setting for pushpin function.
- Listed external app settings, no limit on registration.
- Added setting to external app settings to expand multiple files into parameters instead of launching multiple apps.
- Change button icons to Windows built-in font.
- Configuration data is changed to JSON format, and summarized in Profile folder.
- Folder renaming now merges with existing folders when there is a conflict with an existing folder.
- The clipboard does not react when it is updated with the same text as the file name copied from the application.
- Added Esc to move focus to the search box.
- Migrate project page to [GitHub](https://github.com/neelabo/RealtimeSearch)

### Fixed

- Fixed a bug that could cause incorrect operation when the root folder of a drive is specified as the search folder.
- Fixed a bug that caused an error when deleting a folder.
- Fixed a bug that sometimes hangs when sorting by type.
- Fixed a bug that searches from the clipboard did not remain in the search history.
- Fixed a bug that the index disappears when renaming a file in upper/lower case.

## 3.0
(2019-05-06)

- Case-insensitive regular expression search (/ire).
- Support for date/time-specified search (/since /until).
- Incremental search now works while typing IME.

## 2.0
(2019-02-25)

- Updated environment to .NET Framework 4.7.2.
- New search option rules. Can search by regular expression.
- Fixed a bug that file name capitalization change causes the file to disappear from the search index.

## 1.8
(2018-02-06)

- Updated environment to .NET Framework 4.6.2.
- Introduced kanaxs for hiragana-katakana conversion. Fixed a problem in which “♥” and “? are no longer distinguished.
- Reduced memory usage.

## 1.7
(2017-07-24)

- Changed to control search method by extended keywords.
- Protocol activation support for external applications.
- Fixed a bug that caused processing to stop for non-existent search paths.

## 1.6
(2017-04-03)

- Implemented search keyword order matching option.
- Clipboard file names are now recognized as search keywords.
- Implementation of file extension change warning in file name change.
- Implementation of check for unavailable characters in file renaming.
- Directory renaming no longer takes extensions into account.
- ToolTip is displayed when the detail panel is closed.
- Suppressed tooltip display during renaming.
- Changed internal configuration. Converted search engine section to DLL.

## 1.5
(2016-11-17)

- Installer versions are now available.
- File renaming behavior is now closer to the behavior in Explorer.
- Up to three external applications can be registered.
- Change the detail display in the pop-up to panel. Display ON/OFF by button at lower right.
- Added “word match” and “exact match” flags to search settings.
    - Word Match” searches are based on consecutive letter types (alphabet, kana, kanji, etc.) as words. It is not an exact word search because it does not use Japanese parsing.

## 1.4
(2016-07-05)

- The number of indexes created is now displayed in the in-process display.

## 1.3
(2016-04-24)

### Added
- (Added) Web search button added.
- (Added) Allows external apps to be specified.
- (Added) Added a push-pin feature to search results. Pinned items will remain in the next search.
- (Added) Added “Open in Default App” command.
- (Added) "Delete” command added.
- (Added) Added “Properties” command.
- (Added) Add search keyword history (for the past 6 searches).
- (Changed) Improved search results to immediately reflect file system changes.
- (Changed) Improved indexing speed.
- (Changed) Added saving the order and width of items in the search results list.
- (Changed) Changed file icons to simplified versions. This improves the speed of list display.

## 1.2 
(2016-01-21)

- Added the ability to configure whether folders are displayed in the search results.
- Sorting by file name in natural order.
- More information on popups.
- Added animation during processing.
- Folder selection dialog changed to standard.

## 1.1
(2015-12-13)

- Fixed a bug that clipboard monitoring does not work immediately after startup.
- Clipboard monitoring no longer works on rename dialog.
- Fixed a bug that the context menu shortcut was not displayed.

## 1.0
(2015-11-29)

- First release