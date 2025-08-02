using CsInstall;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  internal class GenericShellExInfrastructure : IDefinition {
    /// <summary>
    /// The MSIX package name.
    /// </summary>
    internal static string MsixPackage { get; } = "GenericShellEx";

    /// <summary>
    /// The MSIX package file.
    /// </summary>
    internal static string MsixPackageFile { get; } = $"{MsixPackage}Package.msix";

    /// <summary>
    /// The certificate file.
    /// </summary>
    internal static string CertificateFile { get; } = "bc4d07566f276942b4377ae213f1d49bceb0ed77.cer";

    /// <summary>
    /// The path to the config file.
    /// </summary>
    internal static string ConfigPath { get; } = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\GenericShellEx");

    /// <summary>
    /// The config file.
    /// </summary>
    internal static string ConfigFile { get; } = "config.json";

    /// <summary>
    /// The name of the DLL.
    /// </summary>
    internal static string Dll { get; } = "GenericShellEx.dll";

    /// <summary>
    /// The DLL registration script.
    /// </summary>
    internal static string RegisterScript { get; } = "Register-GenericShellEx.ps1";

    /// <summary>
    /// Known certificate thumbprints to remove.
    /// </summary>
    /// <remarks>Ensure all entries are lowercased.</remarks>
    internal static readonly HashSet<string> KnownCertificateThumbprints = new() {
      "bc4d07566f276942b4377ae213f1d49bceb0ed77",
      "ddaf333d25b8a30f9ab9cf9e655f5244180ab142"
    };

    public CsInstall.Installer Installer { get; private set; }

    public List<IInstallerTask> InstallerTasks { get; private set; }

    public List<IUninstallerTask> UninstallerTasks { get; private set; }

    public bool UninstallerTasksNeedResources { get; private set; }

    /// <summary>
    /// Initializes <see cref="GenericShellExInfrastructure"/>.
    /// </summary>
    /// <param name="installer">The installer.</param>
    internal GenericShellExInfrastructure(CsInstall.Installer installer) {
      Installer = installer;

      InstallerTasks = new() {
        new ExtractMsixPackage(this),
        new CheckMsixPackageCompatibility(this),
        new InstallCertificate(this),
        new InstallMsixPackage(this),
        new WriteExampleConfigFile(this),
        new RegisterDll(this)
      };

      UninstallerTasks = new() {
        new UnregisterDll(this),
        new UninstallMsixPackage(this),
        new UninstallCertificate(this)
      };

      // We need the registration script to unregister the DLL
      UninstallerTasksNeedResources = true;
    }

    public bool Checks() {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        Installer.Error($"{Installer.ShortName} can only be installed on Windows.");

        return false;
      }

      return true;
    }

    public void InstallChecks() { }

    public void UninstallChecks() { }
  }
}