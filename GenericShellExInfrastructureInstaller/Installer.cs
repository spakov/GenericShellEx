namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// The GenericShellExInfrastructure installer.
  /// </summary>
  public static class Installer {
    internal const string ShortName = "GenericShellExInfrastructure";
    internal const string DisplayName = "Generic Shell Extensions Infrastructure";
    internal const string Publisher = "spakov";
    internal const string Version = "1.0.0.9";
    internal const string Url = "https://github.com/spakov/GenericShellEx";
    internal const string Icon = $"{ShortName}.ico";
    internal const string InstallLocation = $@"%PROGRAMFILES%\{ShortName}";

    /// <summary>
    /// Entrypoint.
    /// </summary>
    /// <param name="args">Command-line parameters.</param>
    /// <returns>0 on success or non-zero on failure.</returns>
    public static int Main(string[] args) {
      CsInstall.Installer installer = new(args) {
        Resources = "Resources",
        InstallLocation = InstallLocation,
        ShortName = ShortName,
        DisplayName = DisplayName,
        Publisher = Publisher,
        Version = Version,
        Url = Url,
        Icon = Icon
      };

      installer.Definition = new GenericShellExInfrastructure(installer);
      return installer.Execute();
    }
  }
}
