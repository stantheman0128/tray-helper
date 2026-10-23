# Contributing

## Build
Windows only, no SDK needed: `build.ps1` compiles with the .NET Framework 4.x `csc` (C# 5, so no `?.`, `$""` or `out var`) and writes `dist\TrayHelper.exe` and `dist\TrayHelper-Setup.exe`.
`deploy-local.ps1` builds `TrayHelper.new.exe`, swaps it in and restarts the running helper; hidden windows are handed over through `hidden.txt`.

## Tests
Run from the repo on Windows. Each suite starts an isolated `--test` instance in `%TEMP%` with throwaway windows and leaves a running TrayHelper alone. Set `$env:TH_SRC` to test a build outside `dist\`.
- `tests\run.ps1`: core (X, Alt+F4, send-to-tray, crash recovery, Store apps)
- `tests\run2.ps1`: 0.8 (ini live reload, Store logos, updater, setup/uninstall)
- `tests\run3.ps1`: 0.9 multi-window (close vs. hide, per-window restore, multi-window crash recovery)
- `tests\excel.ps1`: real Excel with two workbooks against the running helper (Excel must be a managed app); uses its own automation instance

## Internals
- Message window `TrayHelper.msg` (`TrayHelper.test.msg` in `--test`): `0x8002` settings, `0x8003` flyout, `0x8004` exit and restore all, `0x8005` hand-over exit without restoring (updater/setup).
- `hidden.txt`: one line per hidden window, `hwnd, pid, maximized, process, flags, exstyle` (tab separated).
- Low-level mouse/keyboard hooks do the minimum and post to the UI thread. X is detected with `WM_NCHITTEST`, falling back to the caption-button geometry for custom title bars.

## Release
Each change: bump the version (`TrayHelper.cs`, `Setup.cs`), add a `CHANGELOG.md` entry, commit.
Put the built `TrayHelper.exe` and `TrayHelper-Setup.exe` in `/workspace/release/` (or `$RELEASE_DIR`) and run `tools/release.sh <version>`; it uploads both with SHA-256 files. The updater only installs a verified download.

Never commit personal settings (`tray-helper.ini`), logs, screenshots or local paths.
