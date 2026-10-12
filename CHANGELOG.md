# Changelog

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
