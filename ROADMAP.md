# TrayHelper Roadmap

Current stage: **MVP** (speed over polish). Grok CLI will handle the hardening, packaging and test passes later.

## Next
- ~~Crash-loop backoff~~ (done: max 3 relaunches in 5 min, exponential delay).
- Per-app "Restart if it crashes" (default off); Grok Bot always restarts.
- **Installer / uninstaller**: copy to `%LOCALAPPDATA%\TrayHelper`, add a Startup shortcut, migrate the old `GrokBotTray` ini, and remove GrokBotTray.
- **README (EN + zh-TW)**: what it does, screenshots, presets, FAQ (elevated apps, UWP limits).
- **Store/MSIX ExePath auto-detect**: the picker already resolves the AUMID (`shell:AppsFolder\<AUMID>`). Also resolve it for hand-written entries and when a package version changes.
- Per-app tray icons (optional), and better tooltips.
- A fallback for apps with custom title bars that ignore `WM_NCHITTEST` (geometric caption-button mode).
- Do not swallow an X click when the hide is skipped (let it through instead).

## Background auto-update of TrayHelper
- Check GitHub Releases daily for a newer signed build.
- Download it next to the current exe, verify the hash/signature, swap it on the next helper restart (or at idle), and keep one rollback copy.
- Add a setting: auto / notify only / off.

## Native close-to-tray detection (retire our toggle when the vendor ships one)
- Run a periodic check (weekly, plus after the app updates) for each managed app.
  - Use a known-apps manifest (hosted JSON) that maps app → first version with native tray/background support → where the setting lives (e.g. "Settings → General → Keep running in background").
  - Local heuristics: does the app's process own a notification-area icon? Does closing still keep the process alive?
- When native support is detected, mark the app "Native support available". Disable our hook for it (after asking the user), show a one-click guide that opens the official setting, and stop offering our toggle.
- Watch candidates: Grok Bot, Claude Desktop, OpenCode, Hermes Agent, Obsidian.

## Later
- Hotkey to show or hide apps.
- Live reload when the ini is edited by hand.
- Optional code signing to cut AV false positives.
