# Changelog

## 0.8.0
- TrayHelper-Setup.exe: one-file per-user installer and uninstaller (keeps settings, hands hidden windows over to the new version)
- Updates from GitHub Releases: Auto / Notify / Off; SHA-256 verified, keeps the previous version for rollback, no popups
- Store apps hidden with the shortcut show their own logo in the tray
- Launch paths fix themselves: blank paths are filled in from the running app, and Store paths survive package updates
- Apps that turn out to have their own tray icon get a "Use built-in" option that retires TrayHelper's toggle
- Edits to tray-helper.ini apply immediately, without a restart
- Better X detection for custom title bars (uses Windows' own caption-button size)
- Fix: Store apps added from the window list could launch the frame host instead of the app, and matched any Store window
- README in English and Traditional Chinese

## 0.7.2
- Fix: apps that hide their own window (Typeless, ChatGPT…) could pop up after a TrayHelper restart or Exit; TrayHelper now only restores windows it hid itself
- Fix: Store apps hidden on the hidden desktop could still show a taskbar button when the taskbar shows windows from all desktops
- Fix: when an app can't be hidden, Alt+F4 now really closes it (the swallowed Alt no longer puts the app into menu mode)
- `--test` mode for an isolated test instance (own config, no autostart, no built-in app list)

## 0.7.1
- Per-user installer and uninstaller (no admin): installs to %LOCALAPPDATA%\TrayHelper, keeps your settings, restores hidden windows on uninstall
- Code-signing script and file version info (prep for fewer antivirus false positives)

## 0.7.0
- Send any window to the tray with a shortcut (default Ctrl+Alt+Down); its tray icon disappears once you bring it back
- Store / UWP apps are hidden on a separate hidden desktop: no taskbar ghost (Settings → Advanced, on by default)
- The hidden desktop is removed automatically when nothing is on it

## 0.6.1
- Shortcut to show all hidden apps (default Ctrl+Alt+Up), set it with a click-and-press recorder in Settings
- Status dot on each app icon: green = open, grey = hidden in the tray
- Middle-click an app's tray icon to quit it (not Grok Bot)
- Cleaner tray icons: solid backgrounds made transparent, sharp at any scaling

## 0.6.0
- Hidden windows survive a TrayHelper crash: they're re-adopted on the next start, and Exit always brings everything back
- If an app can't be hidden, X / Alt+F4 now close it normally instead of doing nothing
- Smarter main-window matching (process + class + title, ignores splash and helper windows)
- Fix: LINE main window not found; Cursor wrongly treated as having its own tray

## 0.5.2
- Fix: apps restored from the tray came back un-maximized (maximized state is kept across hide/show)

## 0.5.1
- Fix: Settings window reopened at an odd position after closing maximized (saves restore bounds + state, checks the monitor is still there)
- Quit from the tray menu closes the app without flashing its window

## 0.5.0
- Animated drag-to-reorder in the tray panel and the Apps page
- "Open settings" jumps straight to the app's settings page where it has a shortcut (Claude, ChatGPT / Codex, LM Studio: Ctrl+, · Spotify: Ctrl+P)
- Notifications toggle
- Outlook marked Experimental
- One clean Add app dialog: Alt+Tab-accurate window list + Browse .exe
- Settings window remembers its size; opens maximized by default (Advanced option)
- Fix: Excel / Word / PowerPoint closed instead of hiding (Office caption buttons don't answer hit-tests; caption geometry fallback)
- No more "(custom)" label; minimal copy

## 0.4.1
- Add all open windows (bulk add with preview, elevation and Store-app detection)
- TrayHelper itself in the list; Ctrl+Q / Ctrl+W interception
- Close button tracked live across moves, resizes, monitors and DPI
- Fix: TrayHelper's own X swallowed by an app behind it

## 0.4
- Natively supported section with Open settings
- Rescan spinner, Browse .exe and installed apps in Add app
- Fix: VS Code custom title bar X not caught

## 0.3
- Modern WPF tray panel and settings window, per-app tray icons
- App catalog with auto-detect; starts with Windows

## 0.2
- Multi-app TrayHelper with tray-helper.ini

## 0.1 POC
- Grok Bot close-to-tray helper with watchdog
