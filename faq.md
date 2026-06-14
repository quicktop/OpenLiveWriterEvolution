##### Q: What is this?
A: Open Live Writer Evolution is an open source fork of Open Live Writer (itself a fork of Windows Live Writer); an application for authoring, editing, and publishing weblog posts. This fork focuses on modern WordPress compatibility and continued maintenance.

##### Q: How does this differ from Open Live Writer?
A: Open Live Writer Evolution includes bug fixes for WordPress theme detection, improved URL handling for modern WordPress sites, and ongoing improvements for WordPress 5.x/6.x/7.x compatibility. It also supports WordPress Application Passwords out of the box.

##### Q: Does this replace Windows Live Writer?
A: Technically no, spiritually yes. Open Live Writer Evolution is designed to run side-by-side with Windows Live Writer so you can still use Windows Live Writer as you have been.

##### Q: Why the .NET Foundation?
A: The upstream codebase is approximately 200,000 lines of C# so it was a good fit. Take a look at their [About](http://www.dotnetfoundation.org/about) page to learn more.

##### Q: Does it work on older versions of Windows?
A: The primary target is Windows 10 and Windows 11. Windows 7 and Windows 8 may work but are not actively tested. Windows XP is not supported due to the .NET version used.

##### Q: Does it work on a Mac? On Linux?
A: Since .NET runs on Mac OS and Linux, it is possible to port some of the code. But significant portions of the code use Windows-specific APIs (Win32, MSHTML/IE). You are welcome to fork and port.

##### Q: Is this really free?
A: Yes! Open Live Writer Evolution is licensed under the open source [MIT license](license.txt).

##### Q: I found a bug, what do I do?
A: Add to an existing issue or create a new issue via GitHub: https://github.com/quicktop/OpenLiveWriterEvolution/issues/new
   Before creating a new issue, make sure that the issue does not already exist.
   When creating a new issue, identify as much information as you can to assist the developers in fixing the issue. It would be
   helpful to include the log file and any messages you received. The log file is found at %localappdata%\OpenLiveWriter. An easy way
   to get to it is to navigate to File | About | Show log file which is on the lower left of the about dialog box.

##### Q: How can I get involved?
A: WELCOME! We definitely love contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

##### Q: Do Windows Live Writer plug-ins work with Open Live Writer Evolution?
A: Plugins written for the original Windows Live Writer or Open Live Writer should be compatible, as the plugin API has not changed.

##### Q: Can I use WordPress Application Passwords?
A: Yes! Simply enter your WordPress username and use an Application Password (generated in WordPress Admin → Users → Profile → Application Passwords) in the password field. No additional configuration needed.

##### Q: Is this abandonware?
A: No. This fork was created specifically to continue active development and address modern WordPress compatibility issues.

##### Q: How can I support this effort?
A: Here are a few ways you can support the Open Live Writer Evolution effort:
 * Use the product
 * If you like it, tell your friends
 * If you have suggestions, raise an issue
 * Contribute code at [GitHub](https://github.com/quicktop/OpenLiveWriterEvolution)
