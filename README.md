HyperEdit
=========

A plugin for Kerbal Space Program.

To build: Copy Assembly-CSharp.dll, UnityEngine.dll, System.Core.dll, and mscorlib.dll from your KSP install (KSP_Data/Managed) to a folder called "lib" in this directory (root of the git repo)

Then build HyperEdit.sln with xbuild/msbuild/VS/whatever.

----

Note on editing with Visual Studio Code:

Note: In KSP 1.0.4 at least (likely all of KSP versions), UnityEngine.dll references System.dll and mscorlib.dll v2.0.0.0, which the provided dlls are NOT (they are v2.0.5.0). This hecka confuses Visual Studio Code, so I do the following:

    monodis UnityEngine.dll --output=UnityEngine.rewrite.asm
    nano UnityEngine.rewrite.asm # edit AssemblyRef table to correct dll versions/pubkeys
    ilasm -dll UnityEngine.rewrite.asm # creates UnityEngine.rewrite.dll, the fixed dll

The above is why you need to copy System.Core.dll and mscorlib.dll to the lib/ directory instead of using the system-wide one.
