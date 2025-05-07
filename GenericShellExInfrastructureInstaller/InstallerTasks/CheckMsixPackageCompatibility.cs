using CsInstall;
using System;
using System.Xml;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Checks MSIX package compatibility.
  /// </summary>
  internal class CheckMsixPackageCompatibility : IInstallerTask {
    private const string msixPackageManifest = "AppxManifest.xml";
    private const string msixPackageManifestDependencies = "Dependencies";
    private const string msixPackageManifestDependenciesTargetDeviceFamily = "TargetDeviceFamily";
    private const string msixPackageManifestDependenciesTargetDeviceFamilyMinVersion = "MinVersion";

    private const string windowsVersion = "Get-ComputerInfo | Select-Object -ExpandProperty OsHardwareAbstractionLayer";

    public bool SkipInstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="CheckMsixPackageCompatibility"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal CheckMsixPackageCompatibility(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    /// <summary>
    /// <inheritdoc cref="CheckMsixPackageCompatibility"/>
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Checks() {
      Version osVersion;

      // Use a rather convoluted and slow method to get the version of Windows
      // since Environment.OSVersion.Version lies in .NET Framework
      if (!Definition.Installer.PowerShellRun(windowsVersion, out string stdout).Equals(0)) {
        throw new InstallerException("Could not determine Windows version.");
      }

      try {
        osVersion = new(stdout);
      } catch (Exception e) {
        throw new InstallerException("Unable to parse Windows version.", e: e);
      }

      try {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(Definition.Installer.File($@"{GenericShellExInfrastructure.MsixPackage}\{msixPackageManifest}"));

        XmlNode targetDeviceFamily = xmlDocument.DocumentElement![msixPackageManifestDependencies]![msixPackageManifestDependenciesTargetDeviceFamily]!;

        string _minVersion = targetDeviceFamily.Attributes![msixPackageManifestDependenciesTargetDeviceFamilyMinVersion]!.InnerText;
        Version version = new(_minVersion);

        if (osVersion < version) {
          throw new InstallerException($"{Definition.Installer.ShortName} requires Windows version {version} and Windows is version {osVersion}.");
        }
      } catch (Exception e) {
        throw new InstallerException("Unable to validate Windows version.", e: e);
      }

      Definition.Installer.Log("Windows version is OK.");
    }

    public void Install() { }
  }
}