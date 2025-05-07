using CsInstall;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Registers the DLL.
  /// </summary>
  internal class RegisterDll : IInstallerTask {
    public bool SkipInstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="RegisterDll"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal RegisterDll(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    public void Checks() { }

    /// <summary>
    /// <inheritdoc cref="RegisterDll"/>
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Install() {
      if (!Definition.Installer.PowerShellRun(
        Definition.Installer.File(GenericShellExInfrastructure.RegisterScript)
      ).Equals(0)) {
        throw new InstallerException($"Failed to register {GenericShellExInfrastructure.Dll}.");
      }
    }
  }
}