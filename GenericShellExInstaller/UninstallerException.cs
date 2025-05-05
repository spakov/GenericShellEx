using System;

#nullable enable
namespace GenericShellExInstaller {
  /// <summary>
  /// An uninstaller exception.
  /// </summary>
  internal class UninstallerException : Exception {
    /// <remarks>
    /// Writes an error message to the console, unless silent mode is active.
    /// </remarks>
    /// <param name="message">The error message.</param>
    /// <param name="e">An exception to use as an inner exception.</param>
    public UninstallerException(string message, Exception? e = null) : base(message, e) {
      if (!Installer.Silent) {
        if (e is not null) {
          Console.Error.WriteLine($"{message}. {e.Message}");
        } else {
          Console.Error.WriteLine(message);
        }

        Console.Error.WriteLine("Uninstallation failed!");
      }
    }
  }
}
