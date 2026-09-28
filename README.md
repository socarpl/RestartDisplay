# RestartDisplay

Windows x64 console application ported from RestartDisplay.ps1. The original script is retained.

Run `publish\RestartDisplay.exe` without arguments to list monitors and usage.
Run `RestartDisplay.exe 2` to disconnect monitor 2, wait 5 seconds, then reconnect it as an extended display.
The numbers are this application's IDs, not Windows Settings numbers; list again after changing cables or docks.

Place `timeout.txt` beside the EXE containing a nonnegative 32-bit integer (seconds).
Successful reads are announced. Missing, unreadable, invalid, negative, or overflowing values fall back to 5 seconds.
Zero reconnects immediately. The timeout is the period between disconnect and reconnect, not a delay before disconnecting.

`RestartDisplay.exe 2 -l` or `RestartDisplay.exe 2 -link` creates `Restart 2.lnk` beside the EXE without changing displays.
The option can also precede the number. An existing shortcut with that name is replaced.
The shortcut targets this EXE with `2` as its argument; the target is supplied quoted to Windows Shell.
Windows stores shortcut paths separately from arguments and may normalize away the quotes internally.
Paths containing spaces are supported. Shortcut creation does not require that the monitor currently exists.

The program uses QueryDisplayConfig/SetDisplayConfig directly, without PowerShell or external modules.
It refuses to disconnect the last active display, validates layouts before applying them, and attempts reconnection in a finally block.
Ctrl+C shortens the countdown and reconnects. Killing the process or closing its console cannot guarantee reconnection.
Use a local interactive desktop session. Previously disconnected monitors are extended after the wait; duplicated monitors become independent extended displays.
Application window positions are not restored.

## Build

Requires .NET 9 SDK and Visual Studio C++ build tools with a Windows SDK:

```powershell
dotnet publish .\RestartDisplay.csproj -c Release -o .\publish
```

Native AOT compiles this into a standalone Windows x64 executable with size optimization. Distribute only `publish\RestartDisplay.exe`; no .NET installation or PDB is required.

## Review of the original script

The display implementation already used C# and correctly preserved native structure layouts, retried configuration queries, validated disconnect/reconnect configurations, protected the last active display, and attempted original-layout recovery when reconnection failed.
The port retains that behavior, replaces the fixed ten-second wait with the configurable five-second default, adds explicit Ctrl+C handling and shortcut creation, and changes structure-size checks to generic calls suitable for Native AOT.
