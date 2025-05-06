using System;
using System.Collections.Generic;

namespace GenericShellExInstaller {
  internal static class Program {
    internal const string ShortName = "GenericShellEx";
    internal const string DisplayName = "Generic Shell Extensions Infrastructure";
    internal const string Publisher = "spakov";
    internal const string Icon = $"{ShortName}.ico";
    internal const string Version = "1.0.0.3";

    internal const string InstallerCommand = "GenericShellExInstaller";
    internal const string InstallerFile = $"{InstallerCommand}.exe";
    internal const string RegistrySoftwareKey = @"SOFTWARE";
    internal const string RegistryKey = @"Microsoft\Windows\CurrentVersion\Uninstall\spakov.GenericShellExInfrastructure";
    internal const string InstallLocation = $@"%PROGRAMFILES%\{ShortName}";
    internal const string DisplayIcon = $@"{InstallLocation}\{Icon}";
    internal const string UninstallString = $@"{InstallLocation}\{InstallerFile} --uninstall";
    internal const string QuietUninstallString = $"{UninstallString} --silent";
    internal const string UrlInfoAbout = "https://github.com/spakov/GenericShellEx";
    internal const int NoModify = 1;
    internal const int NoRepair = 1;

    internal const string Resources = "Resources";

    internal const string DeveloperModeKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
    internal const string DeveloperModeName = "AllowDevelopmentWithoutDevLicense";
    internal const int DeveloperModeDisabledValue = 0;
    internal const int DeveloperModeEnabledValue = 1;

    internal const string MsixPackage = "GenericShellExPackage";
    internal const string MsixPackageName = $"{MsixPackage}.msix";
    internal const string MsixPackageManifest = "AppxManifest.xml";
    internal const string MsixPackageManifestDependencies = "Dependencies";
    internal const string MsixPackageManifestDependenciesTargetDeviceFamily = "TargetDeviceFamily";
    internal const string MsixPackageManifestDependenciesTargetDeviceFamilyMinVersion = "MinVersion";
    internal const string MsixPackageManifestIdentity = "Identity";
    internal const string MsixPackageManifestIdentityPublisher = "Publisher";
    internal const string MsixPackageManifestIdentityVersion = "Version";

    internal const string CertificateName = "spakov.cer";

    internal const string ConfigName = "config.json";
    internal const string ConfigPath = $@"%LOCALAPPDATA%\{ShortName}";
    internal const string ConfigFile = $@"{ConfigPath}\config.json";

    internal const string RegisterName = "Register-GenericShellEx.ps1";
    internal const string RegisterUnregister = "-Unregister";

    internal const string Comspec = "%COMSPEC%";
    internal const string CompsecArgs = "/c";
    internal const string CompsecArgsPowerShell = "where pwsh > NUL 2>&1";
    internal const string ComspecArgsSelfDelete = $"timeout /t 2 && rmdir /s /q \"{InstallLocation}\"";

    internal const string PowerShell = "powershell";
    internal const string PowerShellWindows = "pwsh";
    internal const string PowerShellSilent = "*> $null";
    internal const string PowerShellParameters = "-NoProfile -ExecutionPolicy Bypass -Command";
    internal const string PowerShellParametersWindowsVersion = "Get-ComputerInfo | Select-Object -ExpandProperty OsHardwareAbstractionLayer";
    internal const string PowerShellParametersGetAppxPackageVersion = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty Version";
    internal const string PowerShellParametersGetAppxPackageFullName = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty PackageFullName";
    internal const string PowerShellParametersRemoveAppxPackage = "Remove-AppxPackage -Package";
    internal const string PowerShellParametersAddAppxPackage = "Add-AppxPackage -Path";

    internal static readonly HashSet<string> KnownCertificateThumbprints = new() {
      "ddaf333d25b8a30f9ab9cf9e655f5244180ab142"
    };

    private static readonly List<string> installOptions = new() {
      "--install",
      "--i",
      "-i",
      "/i"
    };

    private static readonly List<string> uninstallOptions = new() {
      "--uninstall",
      "--u",
      "-u",
      "/u"
    };

    private static readonly List<string> silentOptions = new() {
      "--silent",
      "--s",
      "-s",
      "/s",
      "--q",
      "-q",
      "/q"
    };

    private static readonly List<string> versionOptions = new() {
      "--version",
      "--v",
      "-v",
      "/v"
    };

    private static readonly List<string> helpOptions = new() {
      "--help",
      "--h",
      "-h",
      "/h",
      "--?",
      "-?",
      "/?"
    };

    public static int Main(string[] args) {
      bool install = false;
      bool uninstall = false;
      bool silent = false;
      bool version = false;
      bool help = false;

      foreach (string arg in args) {
        foreach (string installOption in installOptions) {
          if (arg.ToLower().StartsWith(installOption)) install = true;
        }

        foreach (string uninstallOption in uninstallOptions) {
          if (arg.ToLower().StartsWith(uninstallOption)) uninstall = true;
        }

        foreach (string silentOption in silentOptions) {
          if (arg.ToLower().StartsWith(silentOption)) silent = true;
        }

        foreach (string versionOption in versionOptions) {
          if (arg.ToLower().StartsWith(versionOption)) version = true;
        }

        foreach (string helpOption in helpOptions) {
          if (arg.ToLower().StartsWith(helpOption)) help = true;
        }
      }

      if (!install && !uninstall) install = true;

      if (help) {
        Console.Error.WriteLine(
          "Description:\r\n" +
          $"  {DisplayName} installer.\r\n" +
          "\r\n" +
          "Usage:\r\n" +
          $"  {InstallerCommand} [options]\r\n" +
          "\r\n" +
          "Options:\r\n" +
          $"  {installOptions[0]}\tInstall {DisplayName} (default).\r\n" +
          $"  {uninstallOptions[0]}\tUninstall {DisplayName}.\r\n" +
          $"  {silentOptions[0]}\tProduce no output during installation/uninstallation.\r\n" +
          $"  {versionOptions[0]}\tPrint the installer version.\r\n" +
          $"  {helpOptions[0]}\tShow help and usage information."
        );

        return 1;
      }

      if (version) {
        if (!silent) Console.WriteLine(Version);

        return 0;
      }

      if (install && uninstall) {
        if (!silent) Console.Error.WriteLine($"Cannot specify both {installOptions[0]} and {uninstallOptions[0]}");

        return 1;
      }

      try {
        Installer.Install(uninstall: uninstall, silent: silent);
      } catch (InstallerException) {
        return 1;
      } catch (UninstallerException) {
        return 1;
      }

      return 0;
    }
  }
}
