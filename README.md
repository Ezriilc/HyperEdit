HyperEdit
=========

A plugin for Kerbal Space Program.

To build:

Edit HyperEdit.csproj to fix KspInstallDir to point to the correct path (or build with /p:KspInstallDir=the_directory)

Then build HyperEdit.sln with xbuild/msbuild/VS/whatever.

VS2015 is required, or other C#6 compliant C# compiler (khyperia uses mono 4.2.2)
