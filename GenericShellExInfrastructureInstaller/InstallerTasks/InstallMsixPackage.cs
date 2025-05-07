using CsInstall;
using System;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Installs the MSIX package.
  /// </summary>
  internal class InstallMsixPackage : IInstallerTask {
    private const string getAppxPackageVersion = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty Version";
    private const string getAppxPackageFullName = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty PackageFullName";
    private const string addAppxPackage = "Add-AppxPackage -Path {0}";
    private const string removeAppxPackage = "Remove-AppxPackage -Package {0}";

    public bool SkipInstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="InstallMsixPackage"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal InstallMsixPackage(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    public void Checks() { }

    /// <summary>
    /// <inheritdoc cref="InstallMsixPackage"/>
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Install() {
      if (!Definition.Installer.PowerShellRun(
        string.Format(getAppxPackageVersion, GenericShellExInfrastructure.MsixPackage),
        out string msixPackageVersion
      ).Equals(0)) {
        throw new InstallerException($"Could not determine package {GenericShellExInfrastructure.MsixPackage} version.");
      }

      msixPackageVersion = msixPackageVersion.Trim();

      if (msixPackageVersion.Length > 0) {
        Version version;
        Version installedVersion;

        try {
          version = new(Installer.Version);
          installedVersion = new(msixPackageVersion);
        } catch (Exception e) {
          throw new InstallerException($"{GenericShellExInfrastructure.MsixPackage} is already installed but could not determine its version.", e: e);
        }

        if (version == installedVersion) {
          Definition.Installer.Log($"{GenericShellExInfrastructure.MsixPackage} version {installedVersion} is already installed, won't install again");

          return;
        } else if (version < installedVersion) {
          Definition.Installer.Log($"Requested installation for older version ({version}) of package {GenericShellExInfrastructure.MsixPackage} than is installed ({installedVersion}), uninstalling old package");

          try {
            UninstallMsixPackage();
          } catch (UninstallerException e) {
            throw new InstallerException($"Could not remove package {GenericShellExInfrastructure.MsixPackage}.", e: e);
          }
        }
      }

      if (!Definition.Installer.PowerShellRun(
        string.Format(addAppxPackage, Definition.Installer.File(GenericShellExInfrastructure.MsixPackageFile))
      ).Equals(0)) {
        throw new InstallerException($"Could not add package {GenericShellExInfrastructure.MsixPackage}.");
      }

      Definition.Installer.Log($"Added package {GenericShellExInfrastructure.MsixPackage}");
    }

    /// <summary>
    /// Uninstalls the MSIX package.
    /// </summary>
    /// <exception cref="UninstallerException"></exception>
    private void UninstallMsixPackage() {
      if (!Definition.Installer.PowerShellRun(
        string.Format(getAppxPackageFullName, GenericShellExInfrastructure.MsixPackage),
        out string msixPackageFullName
      ).Equals(0)) {
        throw new InstallerException($"Could not determine package {GenericShellExInfrastructure.MsixPackage} full name.");
      }

      msixPackageFullName = msixPackageFullName.Trim();

      if (!Definition.Installer.PowerShellRun(
        string.Format(removeAppxPackage, msixPackageFullName)
      ).Equals(0)) {
        throw new InstallerException($"Could not remove package {GenericShellExInfrastructure.MsixPackage}.");
      }
    }
  }
}