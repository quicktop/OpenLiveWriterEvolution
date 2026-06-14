// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using OpenLiveWriter.Api;
using OpenLiveWriter.ApplicationFramework;
using OpenLiveWriter.BlogClient.Detection;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.HtmlEditor;
using OpenLiveWriter.HtmlParser.Parser;
using OpenLiveWriter.Localization;

namespace OpenLiveWriter.PostEditor.PostHtmlEditing
{
    internal class BlogPostWebViewPreviewEditorControl : UserControl, IBlogPostHtmlEditor, IHtmlEditorCommandSource
    {
        public static bool IsRuntimeAvailable()
        {
            try
            {
                return !String.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
            }
            catch
            {
                return false;
            }
        }

        private readonly WebView2 _webView;
        private readonly CommandManager _commandManager;
        private string _lastTitle = String.Empty;
        private string _lastBodyHtml = String.Empty;
        private string _sourceTitle = String.Empty;
        private string _sourceBodyHtml = String.Empty;
        private bool _isDirty;
        private bool _webViewUnavailable;

        public BlogPostWebViewPreviewEditorControl(CommandManager commandManager)
        {
            _commandManager = commandManager;

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = Color.White;
            Controls.Add(_webView);

            Name = "BlogPostWebViewPreviewEditorControl";
            AccessibleName = "Web Preview";
        }

        public void SetPostHtmlSnapshot(string title, string postBodyHtml)
        {
            _sourceTitle = title ?? String.Empty;
            _sourceBodyHtml = postBodyHtml ?? String.Empty;
        }

        public Control EditorControl
        {
            get { return this; }
        }

        public IHtmlEditorCommandSource CommandSource
        {
            get { return this; }
        }

        public SmartContentEditor CurrentEditor
        {
            get { return null; }
        }

        public IFocusableControl FocusControl
        {
            get { return new FocusableControl(this); }
        }

        public void LoadHtmlFragment(string title, string postBodyHtml, string baseUrl, BlogEditingTemplate editingTemplate)
        {
            _lastTitle = title ?? String.Empty;
            _lastBodyHtml = postBodyHtml ?? String.Empty;

            string titleHtml = HtmlUtils.EscapeEntities(_lastTitle);
            string postHtml = editingTemplate.ApplyTemplateToPostHtml(_lastTitle, titleHtml, _lastBodyHtml);
            postHtml = InsertBaseTag(postHtml, baseUrl);

            string documentPath = TempFileManager.Instance.CreateTempFile("preview.htm");
            using (StreamWriter streamWriter = new StreamWriter(documentPath, false, Encoding.UTF8))
                streamWriter.Write(postHtml);

            NavigateToPreviewFile(documentPath);
        }

        private static string InsertBaseTag(string postHtml, string baseUrl)
        {
            if (String.IsNullOrEmpty(baseUrl))
                return postHtml;

            string baseTag = String.Format(CultureInfo.InvariantCulture, "<base href=\"{0}\">", HtmlUtils.EscapeEntities(baseUrl));
            int headClose = postHtml.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headClose >= 0)
                return postHtml.Insert(headClose, baseTag);

            return postHtml;
        }

        private async void NavigateToPreviewFile(string documentPath)
        {
            if (_webViewUnavailable)
                return;

            try
            {
                if (_webView.CoreWebView2 == null)
                {
                    string userDataFolder = Path.Combine(ApplicationEnvironment.ApplicationDataDirectory, "WebView2");
                    CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    await _webView.EnsureCoreWebView2Async(environment);
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                    _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                }

                _webView.CoreWebView2.Navigate(new Uri(documentPath).AbsoluteUri);
            }
            catch (Exception ex)
            {
                _webViewUnavailable = true;
                Trace.WriteLine("WebView2 preview failed; falling back to blank preview. " + ex);
            }
        }

        public void LoadHtmlFile(string filePath)
        {
            NavigateToPreviewFile(filePath);
        }

        public string GetEditedTitleHtml()
        {
            return _sourceTitle;
        }

        public string GetEditedHtml(bool preferWellFormed)
        {
            return _sourceBodyHtml;
        }

        public string GetEditedHtmlFast()
        {
            return _sourceBodyHtml;
        }

        public string SelectedText
        {
            get { return String.Empty; }
        }

        public string SelectedHtml
        {
            get { return String.Empty; }
        }

        public void EmptySelection()
        {
        }

        public void InsertHtml(string content, bool moveSelectionRight)
        {
        }

        public void InsertHtml(string content, HtmlInsertionOptions options)
        {
        }

        public void InsertLink(string url, string linkText, string linkTitle, string rel, bool newWindow)
        {
        }

        void IBlogPostHtmlEditor.Focus()
        {
            _webView.Focus();
        }

        public void FocusTitle()
        {
            _webView.Focus();
        }

        public void FocusBody()
        {
            _webView.Focus();
        }

        public bool DocumentHasFocus()
        {
            return ContainsFocus;
        }

        public void UpdateEditingContext()
        {
        }

        public void InsertExtendedEntryBreak()
        {
        }

        public void InsertHorizontalLine(bool plainText)
        {
        }

        public void InsertClearBreak()
        {
        }

        public void ChangeSelection(SelectionPosition position)
        {
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }

        public event EventHandler IsDirtyEvent
        {
            add { }
            remove { }
        }

        public bool SuspendAutoSave
        {
            get { return false; }
        }

        public bool FullyEditableRegionActive { get; set; }

        public event EventHandler TitleChanged
        {
            add { }
            remove { }
        }

        public event EventHandler EditableRegionFocusChanged
        {
            add { }
            remove { }
        }

        public void ViewSource()
        {
        }

        public void ClearFormatting()
        {
        }

        public bool CanApplyFormatting(CommandId? commandId)
        {
            return false;
        }

        public string SelectionFontFamily
        {
            get { return String.Empty; }
        }

        public void ApplyFontFamily(string fontFamily)
        {
        }

        public float SelectionFontSize
        {
            get { return 0; }
        }

        public void ApplyFontSize(float fontSize)
        {
        }

        public int SelectionForeColor
        {
            get { return 0; }
        }

        public void ApplyFontForeColor(int color)
        {
        }

        public int SelectionBackColor
        {
            get { return 0; }
        }

        public void ApplyFontBackColor(int? color)
        {
        }

        public string SelectionStyleName
        {
            get { return null; }
        }

        public void ApplyHtmlFormattingStyle(IHtmlFormattingStyle style)
        {
        }

        public bool SelectionBold
        {
            get { return false; }
        }

        public void ApplyBold()
        {
        }

        public bool SelectionItalic
        {
            get { return false; }
        }

        public void ApplyItalic()
        {
        }

        public bool SelectionUnderlined
        {
            get { return false; }
        }

        public void ApplyUnderline()
        {
        }

        public bool SelectionStrikethrough
        {
            get { return false; }
        }

        public void ApplyStrikethrough()
        {
        }

        public bool SelectionSuperscript
        {
            get { return false; }
        }

        public void ApplySuperscript()
        {
        }

        public bool SelectionSubscript
        {
            get { return false; }
        }

        public void ApplySubscript()
        {
        }

        public bool SelectionIsLTR
        {
            get { return false; }
        }

        public void InsertLTRTextBlock()
        {
        }

        public bool SelectionIsRTL
        {
            get { return false; }
        }

        public void InsertRTLTextBlock()
        {
        }

        public EditorTextAlignment GetSelectionAlignment()
        {
            return EditorTextAlignment.None;
        }

        public void ApplyAlignment(EditorTextAlignment alignment)
        {
        }

        public bool SelectionBulleted
        {
            get { return false; }
        }

        public void ApplyBullets()
        {
        }

        public bool SelectionNumbered
        {
            get { return false; }
        }

        public void ApplyNumbers()
        {
        }

        public bool CanIndent
        {
            get { return false; }
        }

        public void ApplyIndent()
        {
        }

        public bool CanOutdent
        {
            get { return false; }
        }

        public void ApplyOutdent()
        {
        }

        public void ApplyBlockquote()
        {
        }

        public bool SelectionBlockquoted
        {
            get { return false; }
        }

        public bool CanInsertLink
        {
            get { return false; }
        }

        public void InsertLink()
        {
        }

        public bool CanRemoveLink
        {
            get { return false; }
        }

        public void RemoveLink()
        {
        }

        public void OpenLink()
        {
        }

        public void AddToGlossary()
        {
        }

        public bool CanPasteSpecial
        {
            get { return false; }
        }

        public bool AllowPasteSpecial
        {
            get { return false; }
        }

        public void PasteSpecial()
        {
        }

        public bool CanFind
        {
            get { return false; }
        }

        public void Find()
        {
        }

        public bool CanPrint
        {
            get { return false; }
        }

        public void Print()
        {
        }

        public void PrintPreview()
        {
        }

        public LinkInfo DiscoverCurrentLink()
        {
            return new LinkInfo(null, null, null, null, false);
        }

        public bool CheckSpelling()
        {
            return true;
        }

        public CommandManager CommandManager
        {
            get { return _commandManager; }
        }

        public bool HasFocus
        {
            get { return ContainsFocus; }
        }

        public bool CanUndo
        {
            get { return false; }
        }

        public void Undo()
        {
        }

        public bool CanRedo
        {
            get { return false; }
        }

        public void Redo()
        {
        }

        public bool CanCut
        {
            get { return false; }
        }

        public void Cut()
        {
        }

        public bool CanCopy
        {
            get { return false; }
        }

        public void Copy()
        {
        }

        public bool CanPaste
        {
            get { return false; }
        }

        public void Paste()
        {
        }

        public bool CanClear
        {
            get { return false; }
        }

        public void Clear()
        {
        }

        public void SelectAll()
        {
        }

        public void InsertEuroSymbol()
        {
        }

        public bool ReadOnly
        {
            get { return true; }
        }

        public event EventHandler CommandStateChanged
        {
            add { }
            remove { }
        }

        public event EventHandler AggressiveCommandStateChanged
        {
            add { }
            remove { }
        }
    }
}
