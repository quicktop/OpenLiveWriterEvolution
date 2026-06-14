Below is a roadmap of Open Live Writer Evolution features and releases

## Inherited from Open Live Writer

### v0.6.3 (baseline)
The version inherited from Open Live Writer at the time this fork was created.

#### Features carried over:
* Multi-platform blog support (WordPress, Blogger, TypePad, etc.)
* WYSIWYG HTML editing via MSHTML
* Image upload and management
* Spell checking
* Plugin extensibility
* Multi-language support (70+ locales)

---

## Open Live Writer Evolution Releases

### v0.7 - WordPress Compatibility
Focus on fixing WordPress-specific issues and modern site compatibility.

#### Bug fixes:
* Fixed WordPress theme detection URL reconstruction (`about:` protocol handling)
* Fixed URL handling to preserve port numbers when rebuilding post URLs
* Removed debug artifact that wrote to hardcoded `c:\temp\docImage.png`
* Fixed `Bitmap` memory leak in background color detection

#### Compatibility:
* WordPress Application Passwords supported out of the box (WordPress 5.6+)
* Tested against WordPress 7.0 XML-RPC endpoint

### v0.8 - Modern Infrastructure (planned)
* Upgrade to .NET Framework 4.8 or .NET 8
* Replace MSHTML editor with WebView2
* CI/CD improvements

### v1.0 - REST API Support (planned)
* WordPress REST API client to replace XML-RPC
* Full Gutenberg/block editor awareness
* OAuth2 authentication support for WordPress.com
