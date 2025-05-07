using CsInstall;
using System;
using System.IO;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Writes the example config file.
  /// </summary>
  internal class WriteExampleConfigFile : IInstallerTask {
    public bool SkipInstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="WriteExampleConfigFile"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal WriteExampleConfigFile(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    /// <summary>
    /// Checks whether the user already has a config file.
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Checks() {
      if (File.Exists(Path.Combine(GenericShellExInfrastructure.ConfigPath, GenericShellExInfrastructure.ConfigFile))) {
        Definition.Installer.Log($"{GenericShellExInfrastructure.ConfigFile} already exists, won't overwrite");
        SkipInstall = true;
      }
    }

    /// <summary>
    /// <inheritdoc cref="WriteExampleConfigFile"/>
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Install() {
      try {
        Definition.Installer.CopyFile(
          GenericShellExInfrastructure.ConfigFile,
          Path.Combine(GenericShellExInfrastructure.ConfigPath, GenericShellExInfrastructure.ConfigFile)
        );
      } catch (Exception e) {
        throw new InstallerException($"Unable to copy {GenericShellExInfrastructure.ConfigFile}.", e: e);
      }

      Definition.Installer.Log($"Copied {GenericShellExInfrastructure.ConfigFile}");
    }
  }
}