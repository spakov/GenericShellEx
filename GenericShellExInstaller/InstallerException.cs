using System;

namespace GenericShellExInstaller {
  /// <summary>
  /// An installer exception.
  /// </summary>
  internal class InstallerException : Exception {
    /// <remarks>
    /// Writes an error message to the console, unless silent mode is active,
    /// and uninstalls.
    /// </remarks>
    /// <param name="message">The error message.</param>
    /// <param name="e">An exception to use as an inner exception.</param>
    public InstallerException(string message, Exception? e = null) : base(message, e) {
      if (!Installer.Silent) {
        if (e is not null) {
          Console.Error.WriteLine($"{message}. {e.Message}");
        } else {
          Console.Error.WriteLine(message);
        }

        Console.Error.WriteLine("Aborted installation!");
      }

      Installer.Install(uninstall: true, Installer.Silent);
    }
  }
}
