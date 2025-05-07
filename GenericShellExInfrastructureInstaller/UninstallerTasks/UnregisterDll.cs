using CsInstall;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Unregisters the DLL.
  /// </summary>
  internal class UnregisterDll : IUninstallerTask {
    private const string unregister = "-Unregister";

    public bool SkipUninstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IUninstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="UnregisterDll"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal UnregisterDll(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    public void Checks() { }

    /// <summary>
    /// <inheritdoc cref="UnregisterDll"/>
    /// </summary>
    /// <exception cref="UninstallerException"></exception>
    public void Uninstall() {
      if (!Definition.Installer.PowerShellRun(
        $"{Definition.Installer.File(GenericShellExInfrastructure.RegisterScript)} {unregister}"
      ).Equals(0)) {
        throw new UninstallerException($"Failed to unregister {GenericShellExInfrastructure.Dll}.");
      }
    }
  }
}