using CsInstall;
using System;
using System.IO.Compression;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Extracts the MSIX package.
  /// </summary>
  internal class ExtractMsixPackage : IInstallerTask {
    public bool SkipInstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="ExtractMsixPackage"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal ExtractMsixPackage(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    public void Checks() { }

    /// <summary>
    /// <inheritdoc cref="ExtractMsixPackage"/>
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Install() {
      try {
        ZipFile.ExtractToDirectory(
          Definition.Installer.File(GenericShellExInfrastructure.MsixPackageFile),
          Definition.Installer.Directory(GenericShellExInfrastructure.MsixPackage)
        );
      } catch (Exception e) {
        throw new InstallerException($"Unable to unzip {GenericShellExInfrastructure.MsixPackageFile}.", e: e);
      }
    }
  }
}