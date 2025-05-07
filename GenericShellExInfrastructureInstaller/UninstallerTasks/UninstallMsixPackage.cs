using CsInstall;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Uninstalls the MSIX package.
  /// </summary>
  internal class UninstallMsixPackage : IUninstallerTask {
    private const string getAppxPackageFullName = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty PackageFullName";
    private const string removeAppxPackage = "Remove-AppxPackage -Package {0}";

    public bool SkipUninstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="UninstallMsixPackage"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal UninstallMsixPackage(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    public void Checks() { }

    /// <summary>
    /// <inheritdoc cref="UninstallMsixPackage"/>
    /// </summary>
    /// <exception cref="UninstallerException"></exception>
    public void Uninstall() {
      if (!Definition.Installer.PowerShellRun(
        string.Format(getAppxPackageFullName, GenericShellExInfrastructure.MsixPackage),
        out string msixPackageFullName
      ).Equals(0)) {
        throw new UninstallerException($"Could not determine package {GenericShellExInfrastructure.MsixPackage} full name.");
      }

      msixPackageFullName = msixPackageFullName.Trim();

      if (!Definition.Installer.PowerShellRun(
        string.Format(removeAppxPackage, msixPackageFullName)
      ).Equals(0)) {
        throw new UninstallerException($"Could not remove package {GenericShellExInfrastructure.MsixPackage}.");
      }

      Definition.Installer.Log($"Removed package {GenericShellExInfrastructure.MsixPackage}");
    }
  }
}