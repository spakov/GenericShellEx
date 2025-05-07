using CsInstall;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  internal class GenericShellExInfrastructure : Definition {
    private const string developerModeKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
    private const string developerModeName = "AllowDevelopmentWithoutDevLicense";
    private const int developerModeDisabledValue = 0;
    private const int developerModeEnabledValue = 1;

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
    internal static string CertificateFile { get; } = "spakov.cer";

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
      "ddaf333d25b8a30f9ab9cf9e655f5244180ab142"
    };

    /// <summary>
    /// Initializes <see cref="GenericShellExInfrastructure"/>.
    /// </summary>
    /// <param name="installer">The installer.</param>
    internal GenericShellExInfrastructure(CsInstall.Installer installer) : base(installer) {
      InstallerTasks.AddRange(
        new List<IInstallerTask>() {
          new ExtractMsixPackage(this),
          new CheckMsixPackageCompatibility(this),
          new InstallCertificate(this),
          new InstallMsixPackage(this),
          new WriteExampleConfigFile(this),
          new RegisterDll(this)
        }
      );

      // We need the registration script to unregister the DLL
      UninstallerTasksNeedResources = true;

      UninstallerTasks.AddRange(
        new List<IUninstallerTask>() {
          new UnregisterDll(this),
          new UninstallMsixPackage(this),
          new UninstallCertificate(this)
        }
      );
    }

    public override bool Checks() {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        Installer.Error($"{Installer.ShortName} can only be installed on Windows.");

        return false;
      }

      return true;
    }

    public override void InstallChecks() {
      if (!((int?) Registry.GetValue(developerModeKey, developerModeName, developerModeDisabledValue)).Equals(developerModeEnabledValue)) {
        throw new InstallerException(
          $"Windows Developer Mode must be enabled to install {Installer.ShortName}. " +
          "See https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development."
        );
      }
    }

    public override void UninstallChecks() { }
  }
}