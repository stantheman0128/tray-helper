# Windows apps: does clicking X quit, and is there close-to-tray? (researched 2026-10-12)
Status: **confirmed-quits** = a source says X quits and there's no setting · **partial** = has a tray icon but X still quits · **has-tray** = built-in, so no preset needed · **unverified** = no source found yet.
Detection fields marked "verify" are best guesses (standard installer paths). Every Electron main window uses the class `Chrome_WidgetWin_1`.

| App | Status | Framework | Detection (process / path / package) | Source |
|---|---|---|---|---|
| VS Code | confirmed-quits (request closed as out of scope, still being asked for in 2026) | Electron | `Code.exe`; `%LOCALAPPDATA%\Programs\Microsoft VS Code` | https://github.com/microsoft/vscode/issues/11723 , https://github.com/microsoft/vscode/issues/33263 |
| Cursor | partial: has a "Show Cursor in system tray" toggle, but X still quits; staff logged the request | Electron (VS Code fork) | `Cursor.exe`; `%LOCALAPPDATA%\Programs\cursor` (verify) | https://forum.cursor.com/t/close-button-x-should-minimize-to-system-tray-instead-of-quitting-windows-agents-window-ide/166005 , https://forum.cursor.com/t/hide-to-system-tray/125082/1 |
| Obsidian | confirmed-quits natively (only a community "Tray" plugin fixes it) | Electron | `Obsidian.exe`; `%LOCALAPPDATA%\Programs\Obsidian` (verify) | https://forum.obsidian.md/t/close-to-tray/6781 , https://github.com/dragonwocky/obsidian-tray |
| Notion | confirmed-quits (official app has no close-to-tray; a third-party build adds it) | Electron | `Notion.exe`; `%LOCALAPPDATA%\Programs\Notion` (verify) | https://www.notionanswers.com/1569/can-notion-be-minimized-or-closed-to-system-tray-on-windows |
| New Outlook | partial: tray icon has "Hide when minimized", but X closes; third-party OutlookToTray exists | WebView2 | `olk.exe`; Appx `Microsoft.OutlookForWindows` (verify) | https://support.microsoft.com/en-us/outlook/minimize-outlook , https://github.com/hookster007/OutlookToTray |
| ChatGPT / Codex desktop | likely-quits: the official docs mention no tray or background option (not confirmed X behavior) | Store app (verify framework) | Appx `OpenAI.ChatGPT-Desktop` (verify), `ChatGPT.exe` (verify) | https://help.openai.com/en/articles/9982051 , https://learn.chatgpt.com/docs/reference/settings |
| Windsurf | likely-quits: no native setting is documented, only a portable launcher's flag | Electron (VS Code fork) | `Windsurf.exe` (verify) | https://github.com/KillTrot/WindsurfPortable |
| Claude Desktop | **conflicting / has-tray**: a Sep 2026 issue says there's no tray, but other issues say X hides to tray when "Show Claude in menu bar" is on, and #90719 even asks to make X quit | Electron, MSIX | Package family `Claude_pzs8sxrjxfjjc`; `claude.exe` | https://github.com/anthropics/claude-code/issues/93612 , https://github.com/anthropics/claude-code/issues/15368 , https://github.com/anthropics/claude-code/issues/90719 |
| LM Studio | has-tray (with "Enable Local LLM Service" on, X hides to tray) | Electron | `LM Studio.exe` (verify) | https://github.com/lmstudio-ai/lms/issues/148 , https://lmstudio.ai/blog/lmstudio-v0.3.5 |
| Ollama | has-tray (background service plus tray icon) | Native/Go | `ollama app.exe` (verify) | https://docs.ollama.com/windows |
| Spotify | has-tray ("Close button should minimize" setting; Alt+F4 still quits) | CEF | `Spotify.exe` | https://community.spotify.com/t5/Desktop-Windows/Close-button-should-minimize-and-Alt-F4/td-p/5910836 |
| Figma | partial/unclear ("Show Figma in system tray" exists, close behavior not documented) | Electron | `Figma.exe` | https://forum.figma.com/report-a-problem-6/notification-39859 |
| Grok Bot | confirmed-quits (we tested it ourselves) | Electron | existing GrokBotTray config | our own testing |
| Perplexity, Grok (xAI app), Teams | unverified: no source found in this pass | - | - | - |

Related: there's a PowerToys request for a generic "override X to tray" feature (https://github.com/microsoft/PowerToys/issues/34649), which shows people want this kind of tool.
