# Open Live Writer Evolution
Open Live Writer Evolution makes it easy to write, preview, and post to your blog.
This is a community-driven fork of Open Live Writer, focused on modern WordPress compatibility and long-term maintenance.

### Installation
Clone or download this repository and build from source. See the **Building** section below.

### Latest News
OpenLiveWriterEvolution is an active fork of Open Live Writer with improvements including:
- Fixed WordPress theme detection compatibility
- Bug fixes in background color detection
- Improved URL handling for modern WordPress sites

For a list of known issues or to report bugs, see the [Issues](https://github.com/quicktop/OpenLiveWriterEvolution/issues) page.

### Contributing
Open Live Writer Evolution is an open source project and welcomes community contributions.
If you would like to help out then please see the [Contributing](CONTRIBUTING.md) guide.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.

### License
Open Live Writer Evolution uses the [MIT License](license.txt).

### History
The product that became Live Writer was originally created by a small, super-talented team of engineers including
JJ Allaire, Joe Cheng, Charles Teague, and Spike Washburn. The team was acquired by Microsoft
in 2006 and organized with the Spaces team. Becky Pezely joined the team and over time, the team grew and shipped
many popular releases of Windows Live Writer.

Microsoft concluded active development with Windows Live Writer 2012. In December 2015 Microsoft donated the code
to the .NET Foundation, and the community released it as Open Live Writer.

Open Live Writer Evolution is a further fork of Open Live Writer, continuing development with a focus on
modern blog platform compatibility, particularly WordPress.

### Building
Open Live Writer Evolution can be built by running `build.cmd` found in this directory.
It can be opened in Visual Studio. The solution is in `src/managed/writer.sln` -- if you see errors in Visual Studio, run `build.cmd` from the command prompt first.
The main program is `src/managed/OpenLiveWriter/ApplicationMain.cs`.
To run from Visual Studio, set the startup project to `OpenLiveWriter`.

**Prerequisites:** Visual Studio 2017 or later (or Build Tools for Visual Studio) with the .NET Framework 4.6.1 Developer Pack and Desktop development with C++ workload.

### .NET Foundation

This project is based on code originally supported by the [.NET Foundation](http://www.dotnetfoundation.org).
