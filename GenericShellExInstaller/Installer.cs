using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

#nullable enable
namespace GenericShellExInstaller {
  internal static class Installer {
    /// <summary>
    /// Whether to operate in silent mode.
    /// </summary>
    public static bool Silent { get; private set; }

    /// <summary>
    /// The executable to use to run PowerShell scripts.
    /// </summary>
    private static string? PowerShell { get; set; }

    /// <summary>
    /// The application publisher.
    /// </summary>
    private static string? Publisher { get; set; }

    /// <summary>
    /// The application version.
    /// </summary>
    private static string? Version { get; set; }

    /// <summary>
    /// Installs GenericShellEx.
    /// </summary>
    /// <remarks>
    /// Uninstalls if <paramref name="uninstall"/> is <c>true</c>.
    /// </remarks>
    /// <param name="uninstall">Whether to uninstall.</param>
    /// <param name="silent"><inheritdoc cref="Silent"
    /// path="/summary"/></param>
    /// <exception cref="InstallerException"></exception>
    internal static void Install(bool uninstall, bool silent = false) {
      Silent = silent;

      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        throw new InstallerException($"{Program.ShortName} can only be installed on Windows.");
      }

      PowerShell = GetPowerShellExecutable();

      if (uninstall) {
        // Exits, does not return
        Uninstall();
      }

      if (!((int?) Registry.GetValue(Program.DeveloperModeKey, Program.DeveloperModeName, Program.DeveloperModeDisabledValue)).Equals(Program.DeveloperModeEnabledValue)) {
        throw new InstallerException(
          $"Windows Developer Mode must be enabled to install {Program.ShortName}. " +
          "See https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development."
        );
      }

      Log($"Installing {Program.ShortName}");

      Assembly @this = Assembly.GetExecutingAssembly();
      string workingDirectory = Mkdtemp();

      string msixPackageResource = $"{@this.GetName().Name}.{Program.Resources}.{Program.MsixPackageName}";
      string certificateResource = $"{@this.GetName().Name}.{Program.Resources}.{Program.CertificateName}";
      string configResource = $"{@this.GetName().Name}.{Program.Resources}.{Program.ConfigName}";
      string registerScriptResource = $"{@this.GetName().Name}.{Program.Resources}.{Program.RegisterName}";
      string iconResource = $"{@this.GetName().Name}.{Program.Resources}.{Program.Icon}";

      string msixPackageFile = WriteMsixPackage(@this, workingDirectory, msixPackageResource);

      ExtractMsixPackageAndCheckWindowsVersion(workingDirectory, msixPackageFile);

      Publisher = GetMsixPackagePublisher(workingDirectory);
      Version = GetMsixPackageVersion(workingDirectory);

      InstallCertificate(@this, workingDirectory, certificateResource);
      InstallMsixPackage(msixPackageFile);
      WriteConfigFile(@this, configResource);
      RegisterDll(@this, workingDirectory, registerScriptResource);
      RegisterInstallation(@this, workingDirectory, iconResource);

      Directory.Delete(workingDirectory, true);

      Log("Installation complete.");
    }

    /// <summary>
    /// Uninstalls GenericShellEx.
    /// </summary>
    /// <remarks>Exits, does not return.</remarks>
    /// <param name="failure">Whether to exit with a failure exit code rather
    /// than a success exit code.</param>
    private static void Uninstall(bool failure = false) {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        throw new UninstallerException($"{Program.ShortName} can only be installed on Windows.");
      }

      Log($"Uninstalling {Program.ShortName}");

      Assembly @this = Assembly.GetExecutingAssembly();
      string workingDirectory = Mkdtemp();

      string registerScriptResource = $"{@this.GetName().Name}.{Program.Resources}.{Program.RegisterName}";

      UnregisterDll(@this, workingDirectory, registerScriptResource);
      UninstallMsixPackage();
      UninstallCertificate();
      UnregisterInstallationAndDelete();

      Directory.Delete(workingDirectory, true);

      Log("Uninstallation complete.");

      Environment.Exit(failure ? 1 : 0);
    }

    /// <summary>
    /// Gets the correct PowerShell executable for this system.
    /// </summary>
    /// <returns>The name of the PowerShell executable.</returns>
    /// <exception cref="InstallerException"></exception>
    private static string GetPowerShellExecutable() {
      ProcessStartInfo processStartInfo;
      Process? process;

      // Run this in the background
      processStartInfo = new() {
        FileName = Environment.ExpandEnvironmentVariables(Program.Comspec),
        Arguments = $"{Program.CompsecArgs} {Program.CompsecArgsPowerShell}",
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new InstallerException($"Could not run {Program.CompsecArgsPowerShell} in %COMSPEC%");
      }

      process.WaitForExit();

      return process.ExitCode.Equals(0)
        ? Program.PowerShellWindows
        : Program.PowerShell;
    }

    /// <summary>
    /// Extracts and writes the MSIX package to a file from an embedded
    /// resource.
    /// </summary>
    /// <param name="this">This assembly.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="msixPackageResource">The name of the resource to
    /// extract.</param>
    /// <returns>The name of the extracted resource.</returns>
    /// <exception cref="InstallerException"></exception>
    private static string WriteMsixPackage(Assembly @this, string workingDirectory, string msixPackageResource) {
      string msixPackageFile;

      StreamReader resourceReader;

      try {
        using (resourceReader = new(@this.GetManifestResourceStream(msixPackageResource)!)) {
          msixPackageFile = $"{workingDirectory}\\{Program.MsixPackageName}";

          using (FileStream msixStream = File.Create(msixPackageFile)) {
            resourceReader.BaseStream.CopyTo(msixStream);
          }
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to extract embedded {Program.MsixPackageName} resource", e);
      }

      return msixPackageFile;
    }

    /// <summary>
    /// Extracts the MSIX package and checks the version of Windows to make
    /// sure the MSIX package can be installed.
    /// </summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="msixPackageFile">The MSIX package file.</param>
    /// <exception cref="InstallerException"></exception>
    private static void ExtractMsixPackageAndCheckWindowsVersion(string workingDirectory, string msixPackageFile) {
      string msixPackageExtractedPath = $"{workingDirectory}\\{Program.MsixPackage}";
      string msixPackageExtractedManifestPath = $"{msixPackageExtractedPath}\\{Program.MsixPackageManifest}";

      // Use a rather convoluted and slow method to get the version of Windows
      // since Environment.OSVersion.Version lies in .NET Framework

      ProcessStartInfo processStartInfo;
      Process? process;
      string stdout;

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {Program.PowerShellParametersWindowsVersion}",
        RedirectStandardOutput = true,
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new InstallerException($"Could not run {Program.PowerShellParametersWindowsVersion} in PowerShell");
      }

      stdout = process.StandardOutput.ReadToEnd().Trim();

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new InstallerException($"{Program.PowerShellParametersWindowsVersion} failed");
      }

      Version osVersion;

      try {
        osVersion = new(stdout);
      } catch (Exception e) {
        throw new InstallerException($"Unable to obtain Windows version", e);
      }

      try {
        ZipFile.ExtractToDirectory(msixPackageFile, msixPackageExtractedPath);

        XmlDocument xmlDocument = new();
        xmlDocument.Load(msixPackageExtractedManifestPath);

        XmlNode targetDeviceFamily = xmlDocument.DocumentElement![Program.MsixPackageManifestDependencies]![Program.MsixPackageManifestDependenciesTargetDeviceFamily]!;

        string _minVersion = targetDeviceFamily.Attributes![Program.MsixPackageManifestDependenciesTargetDeviceFamilyMinVersion]!.InnerText;
        Version version = new(_minVersion);

        if (osVersion < version) {
          throw new InstallerException($"{Program.ShortName} requires Windows version {version} and Windows is only version {Environment.OSVersion.Version}");
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to validate Windows version", e);
      }

      Log("Windows version is OK");
    }

    /// <summary>
    /// Gets the MSIX package version.
    /// </summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>The MSIX package version.</returns>
    /// <exception cref="InstallerException"></exception>
    private static string GetMsixPackageVersion(string workingDirectory) {
      string version;

      string msixPackageExtractedPath = $"{workingDirectory}\\{Program.MsixPackage}";
      string msixPackageExtractedManifestPath = $"{msixPackageExtractedPath}\\{Program.MsixPackageManifest}";

      try {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(msixPackageExtractedManifestPath);

        XmlNode identity = xmlDocument.DocumentElement![Program.MsixPackageManifestIdentity]!;

        version = identity.Attributes![Program.MsixPackageManifestIdentityVersion]!.InnerText;
      } catch (Exception e) {
        throw new InstallerException($"Unable to determine {Program.MsixPackage} version", e);
      }

      return version;
    }

    /// <summary>
    /// Gets the MSIX package publisher.
    /// </summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>The MSIX package publisher.</returns>
    /// <exception cref="InstallerException"></exception>
    private static string GetMsixPackagePublisher(string workingDirectory) {
      string publisher;

      string msixPackageExtractedPath = $"{workingDirectory}\\{Program.MsixPackage}";
      string msixPackageExtractedManifestPath = $"{msixPackageExtractedPath}\\{Program.MsixPackageManifest}";

      try {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(msixPackageExtractedManifestPath);

        XmlNode identity = xmlDocument.DocumentElement![Program.MsixPackageManifestIdentity]!;

        publisher = identity.Attributes![Program.MsixPackageManifestIdentityPublisher]!.InnerText;
        publisher = publisher.Replace("CN=", string.Empty);
      } catch (Exception e) {
        throw new InstallerException($"Unable to determine {Program.MsixPackage} publisher", e);
      }

      return publisher;
    }

    /// <summary>
    /// Installs the certificate from the embedded certificate resource.
    /// </summary>
    /// <param name="this">This assembly.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="certificateResource">The name of the certificate
    /// resource.</param>
    /// <exception cref="InstallerException"></exception>
    private static void InstallCertificate(Assembly @this, string workingDirectory, string certificateResource) {
      string certificateFile;

      StreamReader resourceReader;

      try {
        using (resourceReader = new(@this.GetManifestResourceStream(certificateResource)!)) {
          certificateFile = $"{workingDirectory}\\{Program.CertificateName}";

          using (FileStream certificateStream = File.Create(certificateFile)) {
            resourceReader.BaseStream.CopyTo(certificateStream);
          }
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to extract embedded {Program.CertificateName} resource", e);
      }

      try {
        X509Store x509Store = new(StoreName.Root, StoreLocation.LocalMachine);
        x509Store.Open(OpenFlags.ReadWrite);

        X509Certificate2 certificate = new(X509Certificate2.CreateFromCertFile(certificateFile));

        if (x509Store.Certificates.Contains(certificate)) {
          Log("Certificate already present, won't install again");
        } else {
          x509Store.Add(certificate);

          Log($"Installed certificate {Program.CertificateName}");
        }

        x509Store.Close();
      } catch (Exception e) {
        throw new InstallerException($"Unable to install certificate {Program.CertificateName}", e);
      }
    }

    /// <summary>
    /// Installs the MSIX package.
    /// </summary>
    /// <param name="msixPackageFile">The package to install.</param>
    /// <exception cref="InstallerException"></exception>
    private static void InstallMsixPackage(string msixPackageFile) {
      ProcessStartInfo processStartInfo;
      Process? process;
      string stdout;

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {string.Format(Program.PowerShellParametersGetAppxPackageVersion, Program.ShortName)}",
        RedirectStandardOutput = true,
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new InstallerException($"Could not run {Program.PowerShellParametersGetAppxPackageVersion} in PowerShell");
      }

      stdout = process.StandardOutput.ReadToEnd().Trim();

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new InstallerException($"{Program.PowerShellParametersGetAppxPackageVersion} failed");
      }

      if (stdout.Length > 1) {
        Version version;
        Version installedVersion;

        try {
          version = new(Version!);
          installedVersion = new(stdout);
        } catch (Exception e) {
          throw new InstallerException($"{Program.MsixPackage} is already installed but could not get its version", e);
        }

        if (version == installedVersion) {
          Log($"{Program.MsixPackage} version {installedVersion} is already installed, won't install again");

          return;
        } else if (version < installedVersion) {
          Log($"Requested installation for older version ({version}) than is installed ({installedVersion}), uninstalling old package");

          try {
            UninstallMsixPackage();
          } catch (UninstallerException e) {
            throw new InstallerException($"Could not uninstall {Program.MsixPackage}", e);
          }
        }
      }

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {Program.PowerShellParametersAddAppxPackage} {msixPackageFile} {(Silent ? Program.PowerShellSilent : string.Empty)}",
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new InstallerException($"Could not run {Program.PowerShellParametersAddAppxPackage} in PowerShell");
      }

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new InstallerException($"{Program.PowerShellParametersAddAppxPackage} failed");
      }

      Log($"Installed {Program.MsixPackage}");
    }

    /// <summary>
    /// Writes the example configuration file if it doesn't already exist.
    /// </summary>
    /// <param name="this">This assembly.</param>
    /// <param name="configResource">The name of the config resource.</param>
    /// <exception cref="InstallerException"></exception>
    private static void WriteConfigFile(Assembly @this, string configResource) {
      string configPath = Environment.ExpandEnvironmentVariables(Program.ConfigPath);
      string configFile = Environment.ExpandEnvironmentVariables(Program.ConfigFile);

      if (File.Exists(configFile)) {
        Log($"{configFile} already exists, won't overwrite");

        return;
      }

      if (!Directory.Exists(configPath)) {
        Directory.CreateDirectory(configPath);
      }

      StreamReader resourceReader;

      try {
        using (resourceReader = new(@this.GetManifestResourceStream(configResource)!)) {
          using (FileStream configStream = File.Create(configFile)) {
            resourceReader.BaseStream.CopyTo(configStream);
          }
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to extract embedded {Program.ConfigName} resource", e);
      }

      Log($"Wrote example {Program.ConfigFile}");
    }

    /// <summary>
    /// Registers GenericShellEx.dll.
    /// </summary>
    /// <param name="this">This assembly.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="registerScriptResource">The name of the register script
    /// resource.</param>
    /// <exception cref="InstallerException"></exception>
    private static void RegisterDll(Assembly @this, string workingDirectory, string registerScriptResource) {
      string registerScriptFile;

      StreamReader resourceReader;

      try {
        using (resourceReader = new(@this.GetManifestResourceStream(registerScriptResource)!)) {
          registerScriptFile = $"{workingDirectory}\\{Program.RegisterName}";

          using (FileStream registerScriptStream = File.Create(registerScriptFile)) {
            resourceReader.BaseStream.CopyTo(registerScriptStream);
          }
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to extract embedded {Program.RegisterName} resource", e);
      }

      ProcessStartInfo processStartInfo;
      Process? process;

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {registerScriptFile} {(Silent ? Program.PowerShellSilent : string.Empty)}",
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new InstallerException($"Could not run {registerScriptFile} in PowerShell");
      }

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new InstallerException($"{registerScriptFile} failed");
      }
    }

    /// <summary>
    /// Registers the installation in Windows.
    /// </summary>
    /// <param name="this">This assembly.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="iconResource">The name of the icon resource.</param>
    /// <exception cref="InstallerException"></exception>
    private static void RegisterInstallation(Assembly @this, string workingDirectory, string iconResource) {
      string iconFile;

      StreamReader resourceReader;

      try {
        using (resourceReader = new(@this.GetManifestResourceStream(iconResource)!)) {
          iconFile = $"{workingDirectory}\\{Program.Icon}";

          using (FileStream iconStream = File.Create(iconFile)) {
            resourceReader.BaseStream.CopyTo(iconStream);
          }
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to extract embedded {Program.Icon} resource", e);
      }

      string installLocation = Environment.ExpandEnvironmentVariables(Program.InstallLocation);

      try {
        Directory.CreateDirectory(installLocation);
        File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}\\{Program.InstallerFile}", $"{installLocation}\\{Program.InstallerFile}", overwrite: true);
        File.Copy(iconFile, $"{installLocation}\\{Path.GetFileName(iconFile)}", overwrite: true);
      } catch (Exception e) {
        throw new InstallerException($"Unable to install {Program.DisplayName} to {Program.InstallLocation}", e);
      }

      try {
        RegistryKey softwareRegistryKey = Registry.LocalMachine.OpenSubKey(Program.RegistrySoftwareKey, writable: true)!;
        RegistryKey registryKey = softwareRegistryKey.CreateSubKey(Program.RegistryKey);

        registryKey.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
        registryKey.SetValue("DisplayName", Program.DisplayName);
        registryKey.SetValue("DisplayVersion", Version!);
        registryKey.SetValue("Publisher", Publisher!);
        registryKey.SetValue("InstallLocation", Environment.ExpandEnvironmentVariables(Program.InstallLocation));
        registryKey.SetValue("DisplayIcon", Environment.ExpandEnvironmentVariables(Program.DisplayIcon));
        registryKey.SetValue("UninstallString", Environment.ExpandEnvironmentVariables(Program.UninstallString));
        registryKey.SetValue("QuietUninstallString", Environment.ExpandEnvironmentVariables(Program.QuietUninstallString));
        registryKey.SetValue("URLInfoAbout", Program.UrlInfoAbout);
        registryKey.SetValue("NoModify", Program.NoModify);
        registryKey.SetValue("NoRepair", Program.NoRepair);

        registryKey.Close();
        softwareRegistryKey.Close();
      } catch (Exception e) {
        throw new InstallerException($"Unable to register installation in registry", e);
      }
    }

    /// <summary>
    /// Unregisters GenericShellEx.dll.
    /// </summary>
    /// <param name="this">This assembly.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="registerScriptResource">The name of the register script
    /// resource.</param>
    /// <exception cref="UninstallerException"></exception>
    private static void UnregisterDll(Assembly @this, string workingDirectory, string registerScriptResource) {
      string registerScriptFile;

      StreamReader resourceReader;

      try {
        using (resourceReader = new(@this.GetManifestResourceStream(registerScriptResource)!)) {
          registerScriptFile = $"{workingDirectory}\\{Program.RegisterName}";

          using (FileStream registerScriptStream = File.Create(registerScriptFile)) {
            resourceReader.BaseStream.CopyTo(registerScriptStream);
          }
        }
      } catch (Exception e) {
        throw new UninstallerException($"Unable to extract embedded {Program.RegisterName} resource", e);
      }

      ProcessStartInfo processStartInfo;
      Process? process;

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {registerScriptFile} {Program.RegisterUnregister} {(Silent ? Program.PowerShellSilent : string.Empty)}",
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new UninstallerException($"Could not run {registerScriptFile} {Program.RegisterUnregister} in PowerShell");
      }

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new UninstallerException($"{registerScriptFile} failed");
      }
    }

    /// <summary>
    /// Uninstalls the MSIX package.
    /// </summary>
    /// <exception cref="UninstallerException"></exception>
    private static void UninstallMsixPackage() {
      ProcessStartInfo processStartInfo;
      Process? process;
      string stdout;

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {string.Format(Program.PowerShellParametersGetAppxPackageFullName, Program.ShortName)}",
        RedirectStandardOutput = true,
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new UninstallerException($"Could not run {Program.PowerShellParametersGetAppxPackageFullName} in PowerShell");
      }

      stdout = process.StandardOutput.ReadToEnd().Trim();

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new UninstallerException($"{Program.PowerShellParametersGetAppxPackageFullName} failed");
      }

      if (stdout.Length < 1) {
        return;
      }

      processStartInfo = new() {
        FileName = Program.PowerShell,
        Arguments = $"{Program.PowerShellParameters} {Program.PowerShellParametersRemoveAppxPackage} {stdout} {(Silent ? Program.PowerShellSilent : string.Empty)}",
        UseShellExecute = false
      };

      process = Process.Start(processStartInfo);

      if (process is null) {
        throw new UninstallerException($"Could not run {Program.PowerShellParametersRemoveAppxPackage} in PowerShell");
      }

      process.WaitForExit();

      if (!process.ExitCode.Equals(0)) {
        throw new UninstallerException($"{Program.PowerShellParametersRemoveAppxPackage} failed");
      }
    }

    /// <summary>
    /// Uninstalls known certificates.
    /// </summary>
    /// <exception cref="UninstallerException"></exception>
    private static void UninstallCertificate() {
      try {
        X509Store x509Store = new(StoreName.Root, StoreLocation.LocalMachine);
        x509Store.Open(OpenFlags.ReadWrite);

        List<X509Certificate2> certificatesToRemove = new();

        foreach (X509Certificate2 certificate in x509Store.Certificates) {
          if (Program.KnownCertificateThumbprints.Contains(certificate.Thumbprint.ToLower())) {
            certificatesToRemove.Add(certificate);
          }
        }

        foreach (X509Certificate2 certificate in certificatesToRemove) {
          Log($"Removing certificate with thumbprint {certificate.Thumbprint}");

          x509Store.Remove(certificate);
        }

        x509Store.Close();
      } catch (Exception e) {
        throw new UninstallerException($"Unable to uninstall certificate", e);
      }
    }

    /// <summary>
    /// Unregisters the installation in Windows and deletes the installer
    /// directory.
    /// </summary>
    /// <remarks>
    /// Leaves two seconds before %COMSPEC% deletes the installer
    /// directory.</remarks>
    /// <exception cref="InstallerException"></exception>
    private static void UnregisterInstallationAndDelete() {
      try {
        RegistryKey softwareRegistryKey = Registry.LocalMachine.OpenSubKey(Program.RegistrySoftwareKey, writable: true)!;

        softwareRegistryKey.DeleteSubKeyTree(Program.RegistryKey, throwOnMissingSubKey: false);
        softwareRegistryKey.Close();
      } catch (Exception e) {
        throw new UninstallerException($"Unable to unregister installation in registry", e);
      }

      // Run this in the background
      ProcessStartInfo processStartInfo = new() {
        FileName = Environment.ExpandEnvironmentVariables(Program.Comspec),
        Arguments = $"{Program.CompsecArgs} {Program.ComspecArgsSelfDelete}",
        UseShellExecute = true,
        WindowStyle = ProcessWindowStyle.Hidden
      };

      Process.Start(processStartInfo);
    }

    /// <summary>
    /// Logs a message, unless silent mode is active.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private static void Log(string message) {
      if (!Silent) Console.WriteLine(message);
    }

    /// <summary>
    /// Creates a unique temporary directory.
    /// </summary>
    /// <returns>The name of the directory.</returns>
    private static string Mkdtemp() {
      string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

      if (Directory.Exists(directory)) {
        return Mkdtemp();
      } else {
        Directory.CreateDirectory(directory);
        return directory;
      }
    }
  }
}