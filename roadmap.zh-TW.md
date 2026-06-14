以下是 Open Live Writer Evolution 的功能與版本釋出規劃藍圖。

## 繼承自 Open Live Writer

### v0.6.3 (基準版本)
建立此分支時，繼承自 Open Live Writer 的基礎版本。

#### 保留下來的功能：
* 多平台部落格支援（WordPress、Blogger、TypePad 等）
* 透過 MSHTML 進行所見即所得（WYSIWYG）的 HTML 編輯
* 圖片上傳與管理
* 拼字檢查
* 外掛程式擴充性
* 多國語系支援（70 多種語言環境）

---

## Open Live Writer Evolution 版本釋出

### v0.7 - WordPress 相容性
專注於修正 WordPress 專屬問題與現代網站相容性。

#### Bug 修正：
* 修正 WordPress 佈景主題偵測時的網址重建問題（處理 `about:` 協定）
* 修正網址處理，以在重建文章網址時保留連接埠號碼
* 移除了先前寫入寫死路徑 `c:\temp\docImage.png` 的偵錯殘留檔案
* 修正背景顏色偵測中的 `Bitmap` 記憶體洩漏問題

#### 相容性：
* 原生支援 WordPress 「應用程式密碼」（Application Passwords）功能（WordPress 5.6+）
* 已針對 WordPress 7.0 XML-RPC 端點進行測試

### v0.8 - 現代化基礎架構（規劃中）
* 升級至 .NET Framework 4.8 或 .NET 8
* 使用 WebView2 取代 MSHTML 編輯器
* CI/CD 持續整合與部署的改進

### v1.0 - REST API 支援（規劃中）
* 實作 WordPress REST API 用戶端以取代舊有的 XML-RPC
* 全面感知並整合 Gutenberg / 區塊編輯器
* 支援 WordPress.com 的 OAuth2 驗證
