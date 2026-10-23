# TrayHelper

**關掉視窗，App 繼續跑。** · [English](README.md)

TrayHelper 是 Windows 10/11 的系統匣小工具。有些 App 按 X（或 Alt+F4）就會整個結束，TrayHelper 會把它們的視窗收進系統匣，讓 App 在背景繼續執行；點一下它的系統匣圖示就能叫回來。

非官方、MIT 授權、只裝在目前使用者（不需系統管理員）。

## 功能
- **任何 App 都能關閉縮到系統匣**：X 按鈕、Alt+F4，可選 Ctrl+Q / Ctrl+W。
- **多視窗 App 跟 macOS 一樣**：開著好幾個視窗時按 X 會真的關掉那一個，只有最後一個才收進系統匣。藏了好幾個視窗時，右鍵選單會逐一列出可個別叫回，另有「Restore all」。
- **每個被管理的 App 各有一個系統匣圖示**：左鍵顯示／隱藏、右鍵選單、中鍵結束該 App。
- **快捷鍵把任何視窗送進系統匣**（預設 **Ctrl+Alt+Down**），視窗叫回後圖示自動消失。
- **一鍵顯示所有隱藏的 App**（預設 **Ctrl+Alt+Up**）。兩組快捷鍵都在設定裡點一下、按鍵錄製。
- **Microsoft Store / UWP App**（小算盤、相片…）藏到一個隱藏的虛擬桌面：沒有空白框、工作列不留殘影；系統匣圖示用該 App 自己的商店 Logo。
- **不怕當掉**：隱藏的視窗記在 `hidden.txt`，TrayHelper 當掉或重開後會重新接手；**按 Exit 一定把所有視窗還原**。
- **藏不起來就正常關閉**：視窗無法隱藏時，關閉動作照常進行。
- **聰明對到主視窗**：依程序、視窗類別、標題比對，略過啟動畫面與輔助視窗。
- **自動偵測支援的 App**（Grok Bot、VS Code、Cursor、Windsurf、Obsidian、Notion、Thunderbird、Outlook…）；本身就有常駐功能的（Claude、ChatGPT/Codex、Spotify、LINE、Typeless、LM Studio）列在「Natively supported」，附「Open settings」直達設定。
- **偵測原生系統匣功能**：被管理的 App 若本身就有系統匣圖示，設定頁會出現「Use built-in」，可改用 App 自己的功能、收掉 TrayHelper 的開關；不跳通知。
- **啟動路徑自動修復**：路徑空白或失效時（包含商店 App 每次更新都會變的路徑），從執行中的 App 自動補正。
- **設定即時生效**：手動編輯 `tray-helper.ini` 存檔就套用。
- **自動更新**（GitHub Releases）：Auto／Notify（預設）／Off；下載後驗證 SHA-256，舊版保留一份可回退。

## 安裝
1. 從 Releases 下載 `TrayHelper-Setup.exe` 執行：裝到 `%LOCALAPPDATA%\TrayHelper`、加入開機啟動與開始功能表；升級時保留設定。
2. 解除安裝：**設定 → 應用程式 → 已安裝的應用程式**，或在安裝資料夾執行 `Uninstall.exe --uninstall`（會先還原所有隱藏視窗）。

安裝程式參數：`--silent`、`--dir <路徑>`、`--no-run-key`、`--no-start`、`--uninstall`。

## 使用
| 位置 | 操作 |
|---|---|
| TrayHelper 圖示 | 左鍵：設定 · 右鍵：快速面板 |
| App 的系統匣圖示 | 左鍵：顯示/隱藏 · 右鍵：選單 · 中鍵：結束 App |
| 任何地方 | **Ctrl+Alt+Down** 把目前視窗送進系統匣 · **Ctrl+Alt+Up** 顯示所有隱藏的 App |

在 **設定 → Apps** 加入 App：「Add app…」加單一 App，「Add all open windows…」一次加入多個。

## 設定檔（exe 旁的 `tray-helper.ini`）
欄位同英文版 README：`HotkeyRestoreAll`、`HotkeySendToTray`、`UwpVirtualDesktop`、`Updates`（auto/notify/off）、`UpdateRepo`，以及每個 `[App:名稱]` 的 `Enabled`、`ExePath`（Store App 用 `shell:AppsFolder\<AUMID>`，空白會自動補）、`ProcessName`（`a|b` 任一）、`WindowClass`、`WindowTitle`（`*x` 包含、`x*` 開頭）、`InterceptCtrlQ/W`、`Geometric`。

## 常見問題
- **以系統管理員身分執行的 App 沒反應？** Windows 不允許一般程式攔截提升權限的 App；請也用系統管理員身分執行 TrayHelper，或不要管理那個 App。
- **自繪標題列的 App 按 X 沒攔到？** TrayHelper 會自動改用標準標題列按鈕位置判斷；仍然不行就在該 App 設 `Geometric=true`。
- **防毒軟體警告？** exe 尚未數位簽章；有憑證後可用 `installer\sign.ps1` 簽署。
- **中鍵會不小心關掉 Grok Bot 嗎？** 不會：Grok Bot 停用中鍵結束。

## 自行編譯
在 Windows 上，不需要 SDK（使用 .NET Framework 4.x 內建的 `csc`）：
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1          # 產生 dist\TrayHelper.exe 與 dist\TrayHelper-Setup.exe
powershell -ExecutionPolicy Bypass -File tests\run.ps1      # 以獨立的 --test 執行個體自動測試
```

第三方元件：[VirtualDesktopAccessor](https://github.com/Ciantic/VirtualDesktopAccessor)（MIT），見 THIRD-PARTY.md。
