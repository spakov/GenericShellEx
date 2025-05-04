# GenericShellEx
Generic shell extension for `IExplorerCommand` context menu entries in
Windows 11.

## Description
Add a custom handler to the Windows 11 "new" right-click Explorer context menu.
Supports types `*` (all files), `Directory` (right-clicking a directory), and
`Directory\Background` (right-clicking the background of the window while
inside a directory). Use a JSON configuration file to define the handler for
each type.

## Configuration
GenericShellEx uses a simple JSON configuration file located at
`%LOCALAPPDATA%\GenericShellEx\config.json`:

```
{
  "types": {
    "*": {
      "title": "Open in Neovim",
      "icon": "%PROGRAMFILES%\\Neovim\\bin\\nvim.exe,0",
      "toolTip": "Open in Neovim",
      "command": "wt --size 164,48 nt --profile Neovim nvim %*"
    },
    "Directory": {
      "title": "Open in Neovim",
      "icon": "%PROGRAMFILES%\\Neovim\\bin\\nvim.exe,0",
      "toolTip": "Open in Neovim",
      "command": "wt --size 224,64 nt --profile Neovim nvim %1"
    },
    "Directory\\Background": {
      "title": "Open in Neovim",
      "icon": "%PROGRAMFILES%\\Neovim\\bin\\nvim.exe,0",
      "toolTip": "Open in Neovim",
      "command": "wt --size 224,64 nt --profile Neovim nvim %1"
    }
  }
}
```

The first-level property names within `types` are the shell type that should be
associated with the shell extension. Within each of those objects, there are
four properties:
- `title` sets the title of the context menu entry.
- `icon` sets the icon of the context menu entry in the standard format of the
  path to some kind of compiled code unit, a comma, and the index of the
  appropriate icon group resource within it. (This presumably works with a
  `.ico` as well, but I haven't tested it.)
- `toolTip` sets the tooltip that is associated with the context menu entry,
  but these appear to be unused in the Windows 11 Explorer right-click context
  menu.
- `command` sets the command to execute.

Two variables are supported in the `command` property:
- `%*`, which expands to all selected filenames, quoted.
- `%1`, which expands to the first selected filename, quoted.

An optional top-level `logFile` property is supported with the path to a log
file:

```
  "logFile": "%LOCALAPPDATA%\GenericShellEx\GenericShellEx.log"
```

However, Windows 11 appears to detect shell extensions that behave "badly" and
sometimes prevents them from presenting their context menu entries. Logging is
useful for troubleshooting but definitely prevents the shell extension from
working reliably.

## License
GenericShellEx is released under the MIT License. It also uses
[nlohmann/json](https://github.com/nlohmann/json), which is also licensed under
the MIT License.

## Background
Windows 11 is incredibly finicky about allowing items to be added to the "new"
right-click Explorer context menu. The classic `HKEY_CLASSES_ROOT` is still
supported, but only for the "Show more options" menu. In short, the only way to
achieve this is to use an MSIX package with the `<desktop4:Extension
Category="windows.fileExplorerContextMenus">` extension. However, this has
its limitations.

### Limitations
In MSIX packages, the `IExplorerCommand` interface is supported and the
`IExplorerCommandProvider` interface is not supported. What this boils down to
is that you get one top-level context item per type per CLSID. There is not any
clever way to work around this by dynamically registering CLSIDs since
sandboxed Windows Apps are prohibited from modifying `HKEY_CLASSES_ROOT`.

`GenericShellEx.dll` cannot register itself in a sandboxed environment and
therefore I didn't bother implementing `DllRegisterServer` and
`DllUnregisterServer` for `regsvr32`. Instead, I wrote
`Register-GenericShellEx.ps1` to do this. Invoke with no parameters to register
and with `-Unregister` to unregister.

MSIX packages must have an executable associated with them. Therefore, I've
created `FullTrustStub.exe`, which literally does nothing. MSIX packages must
also be signed (unless installed as described in [Create an unsigned MSIX
package](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package)).
I generated a self-signed code signing certificate using
`New-SelfSignedCertificate -Subject "CN=spakov" -Type CodeSigningCert
-CertStoreLocation "Cert:\CurrentUser\My"` and sign the package in
`GenericShellExPackage.wapproj` in the `Package` target using the certificate's
thumbprint.

If you want more than one context menu entry, generate new CLSIDs, build a new
`GenericShellEx.dll` that uses those CLSIDs, and package into another MSIX
package with a unique name.

### Alternatives
I have found a number of other solutions that do something similar but approach
this rather annoying problem in a different way:

- [ikas-mc/ContextMenuForWindows11](https://github.com/ikas-mc/ContextMenuForWindows11):
truly customizable items presented in a submenu.
- [Easy Context Menu](https://www.sordum.org/7615/easy-context-menu-v1-6/): a
fixed set of items that can be enabled or disabled.
- [Winaero Tweaker](https://winaerotweaker.com/): a fixed set of items that can
be enabled or disabled.
- Revert to the Windows 10-style context menu

All I wanted was a way to launch nvim with a single right click. This achieves
that.

## Releases
A prebuilt x64 MSIX package is available at [Releases](https://github.com/spakov/GenericShellEx/releases).

## Requirements
As configured, this will work on Windows 11 21H2 and newer. This should work on
Windows 10 as well, though there's no reason to do so since there is no Windows
11-style context menu in Windows 10. You'd need to update
`<TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>` in
`GenericShellExPackage.wapproj` to do so.

### Installation
1. Turn on [Windows 11 Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development).
2. Install the `spakov.cer` certificate into Local Machine\Trusted Root
   Certification Authorities.
3. Install the MSIX package. (This can either be done by double-clicking it or
   via `Add-AppPackage`.)
4. Build your `config.json` in `%LOCALAPPDATA%\GenericShellEx`.
5. Run `Register-GenericShellEx.ps1` to register the DLL.
6. Right-click something in Explorer.

### Uninstallation
1. Run `Register-GenericShellEx.ps1 -Unregister` to unregister the DLL.
2. Delete `%LOCALAPPDATA%\GenericShellEx`, if desired.
3. Uninstall the MSIX package.
4. Delete the certificate from Local Machine\Trusted Root Certification
   Authorities.
5. Turn off Windows 11 Developer Mode.

## Building
Build `GenericShellExPackage`, which will build `FullTrustStub.exe` and
`GenericShellEx.dll`, then produce
`GenericShellExPackage\GenericShellExPackage.msix`.
