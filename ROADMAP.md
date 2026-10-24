# TrayHelper Roadmap

Current stage: **MVP** (speed over polish).

## Done in 0.8.0
- ~~Packaged installer~~: `TrayHelper-Setup.exe` (per-user, upgrade in place, migrates a running copy, Start menu + Apps & features entry, `--uninstall`). The 0.7.1 scripts in `installer\` still work.
- ~~README (EN + zh-TW)~~.
- ~~Store/MSIX ExePath auto-detect~~: blank or broken paths are filled in from the running window, and `...\WindowsApps\Pkg_<version>...` paths become the version-independent `shell:AppsFolder\<AUMID>`.
- ~~Geometric caption-button fallback~~ (since 0.5.0, refined with DWM's caption-button size in 0.8.0; force it with `Geometric=true`).
- ~~Background auto-update~~: GitHub Releases, Auto / Notify / Off, SHA-256 verified, the old exe is kept as `TrayHelper.old.exe`, and hidden windows are handed over. **Needs public releases**: the repo is private, so the check finds nothing until releases are public (or `UpdateRepo` points to a public releases repo).
- ~~Native close-to-tray detection~~: a managed app that owns a tray icon (NotifyIconSettings, minus apps known to still quit on X) gets a "Has its own tray icon · Use built-in" suggestion, with no popups. "Use TrayHelper" switches it back.
- ~~Live reload when the ini is edited by hand~~.

## Next
- Code signing: script ready (`installer\sign.ps1`); needs a certificate. Sign both exes before publishing releases.
- Hosted known-apps manifest (app → first version with native tray support → where the setting is), so native detection doesn't rely on the tray-icon heuristic alone.
- ~~Demo GIF~~ (`assets/demo.gif` + short clips on the About page and README; private bits blurred).

## Notes
- Keyboard interception: Alt+F4 is always intercepted for apps with close-to-tray on. Ctrl+Q / Ctrl+W are intercepted only when the app's main window is in the foreground, and only when enabled per app (`InterceptCtrlQ=true` / `InterceptCtrlW=true`). Ctrl+Q is on by default for Thunderbird.
- Close-button geometry is recalculated whenever a managed window moves or resizes (WinEvent LOCATIONCHANGE, 60 ms debounce), and every second as a backstop.
- The watchdog only runs, silently, for Grok Bot. No other app is auto-restarted.
- Store / UWP windows (ApplicationFrameHost) are hidden on a separate hidden desktop ("TrayHelper"), which is removed when empty. Their taskbar button is dropped too, for taskbars that show all desktops. If the virtual-desktop helper isn't available, Add all open windows leaves Store apps unticked.
- Elevated apps are flagged "needs TrayHelper run as admin".
