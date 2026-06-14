##### Q：這是什麼？
A：Open Live Writer Evolution 是 Open Live Writer（其本身是 Windows Live Writer 的社群分支）的一個開源分支，這是一個用於撰寫、編輯和發布網頁部落格文章的應用程式。此分支專注於現代 WordPress 相容性以及持續的維護。

##### Q：這與 Open Live Writer 有何不同？
A：Open Live Writer Evolution 包含了 WordPress 佈景主題偵測的 Bug 修正、針對現代 WordPress 網站改善的網址處理方式，以及針對 WordPress 5.x/6.x/7.x 相容性的持續改進。它也原生支援 WordPress 「應用程式密碼」（Application Passwords）功能。

##### Q：這會取代 Windows Live Writer 嗎？
A：技術上來說不會，但精神上是的。Open Live Writer Evolution 設計成可與 Windows Live Writer 並存，因此您仍然可以像以前一樣使用 Windows Live Writer。

##### Q：為什麼要加入 .NET 基金會？
A：因為上游的程式碼庫大約有 20 萬行 C# 程式碼，所以非常適合。您可以查看他們的 [.NET 基金會關於頁面](http://www.dotnetfoundation.org/about) 以瞭解更多資訊。

##### Q：它能在舊版本的 Windows 上運作嗎？
A：主要目標是 Windows 10 和 Windows 11。Windows 7 和 Windows 8 可能可以運作，但並未主動進行測試。由於所使用的 .NET 版本限制，不支援 Windows XP。

##### Q：它能在 Mac 或 Linux 上執行嗎？
A：由於 .NET 可在 macOS 和 Linux 上執行，因此有可能移植部分程式碼。但是，該程式碼有很大一部分使用了 Windows 專屬的 API（Win32、MSHTML/IE）。非常歡迎您派生（Fork）並自行移植。

##### Q：這真的是免費的嗎？
A：是的！Open Live Writer Evolution 是採用開源的 [MIT 授權條款](license.txt) 進行授權。

##### Q：我發現了 Bug，該怎麼辦？
A：請在 GitHub 上加入現有的 Issue 或建立一個新的 Issue：https://github.com/quicktop/OpenLiveWriterEvolution/issues/new 。
   在建立新 Issue 之前，請確認該問題是否已經存在。
   建立新 Issue 時，請提供儘可能多的資訊，以協助開發人員修正問題。如果能附上記錄檔和您收到的任何錯誤訊息，將會非常有幫助。記錄檔位於 `%localappdata%\OpenLiveWriter` 下。您可以透過點選關於對話框左下角的「檔案」|「關於」|「顯示記錄檔」輕鬆開啟它。

##### Q：我該如何參與？
A：歡迎加入！我們非常期待社群的貢獻！詳細資訊請參閱 [CONTRIBUTING.zh-TW.md](CONTRIBUTING.zh-TW.md)。

##### Q：Windows Live Writer 的外掛程式適用於 Open Live Writer Evolution 嗎？
A：為原始 Windows Live Writer 或 Open Live Writer 撰寫的外掛應該都能相容，因為外掛 API 並沒有變更。

##### Q：我可以使用 WordPress 應用程式密碼嗎？
A：可以！只需在密碼欄位中輸入您的 WordPress 使用者名稱並填入「應用程式密碼」（可在 WordPress 管理後台的「使用者」→「個人設定」→「應用程式密碼」中產生）即可，無需進行任何額外設定。

##### Q：這是被遺棄的軟體（Abandonware）嗎？
A：不是的。此分支的建立專門為了繼續主動開發，並解決與現代 WordPress 之間的相容性問題。

##### Q：我該如何支持這項工作？
A：以下是幾種您可以支持 Open Live Writer Evolution 的方式：
 * 使用這款軟體。
 * 如果您喜歡它，請推薦給您的朋友。
 * 如果您有任何建議，請建立 Issue。
 * 在 [GitHub](https://github.com/quicktop/OpenLiveWriterEvolution) 上貢獻程式碼。
