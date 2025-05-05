using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;

namespace GenericShellExInstaller {
  public static class Program {
    internal const string ShortName = "GenericShellEx";
    internal const string DisplayName = "Generic Shell Extensions Infrastructure";
    internal const string Icon = $"{ShortName}.ico";

    internal const string InstallerFile = "GenericShellExInstaller.exe";
    internal const string RegistrySoftwareKey = @"SOFTWARE";
    internal const string RegistryKey = @"Microsoft\Windows\CurrentVersion\Uninstall\spakov.GenericShellExInfrastructure";
    internal const string InstallLocation = $@"%PROGRAMFILES%\{ShortName}";
    internal const string DisplayIcon = $@"{InstallLocation}\{Icon}";
    internal const string UninstallString = $@"{InstallLocation}\{InstallerFile} --uninstall";
    internal const string QuietUninstallString = $"{UninstallString} --silent";
    internal const string UrlInfoAbout = "https://github.com/spakov/GenericShellEx";
    internal const int NoModify = 1;
    internal const int NoRepair = 1;

    internal const string SelfDeleteComspec = "%COMSPEC%";
    internal const string SelfDeleteComspecArgs = $"/C timeout /t 2 && rmdir /s /q \"{InstallLocation}\"";

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

    internal const string PowerShell = "pwsh";
    internal const string PowerShellSilent = "*> $null";
    internal const string PowerShellParameters = "-NoProfile -ExecutionPolicy Bypass -Command";
    internal const string PowerShellParametersGetAppxPackageVersion = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty Version";
    internal const string PowerShellParametersGetAppxPackageFullName = "Get-AppxPackage -Name {0} | Select-Object -ExpandProperty PackageFullName";
    internal const string PowerShellParametersRemoveAppxPackage = "Remove-AppxPackage -Package";
    internal const string PowerShellParametersAddAppxPackage = "Add-AppxPackage -Path";

    internal static readonly HashSet<string> KnownCertificateThumbprints = [
      "ddaf333d25b8a30f9ab9cf9e655f5244180ab142"
    ];

    public static async Task<int> Main(string[] args) {
      int exitCode = 0;

      RootCommand rootCommand;

      Option<bool> installOption = new(
        name: "--install",
        description: $"Installs {ShortName}.",
        parseArgument: (_) => true,
        isDefault: true
      );

      Option<bool> uninstallOption = new(
        name: "--uninstall",
        description: $"Uninstalls {ShortName}."
      );

      Option<bool> silentOption = new(
        name: "--silent",
        description: "Produce no output during installation/uninstallation."
      );

      rootCommand = new(
        description: $"{ShortName} installer."
      ) {
        installOption,
        uninstallOption,
        silentOption
      };

      rootCommand.SetHandler(
        (uninstall, silent) => {
          try {
            Installer.Install(uninstall, silent);
          } catch (InstallerException) {
            exitCode = 1;
          } catch (UninstallerException) {
            exitCode = 2;
          }
        },
        uninstallOption,
        silentOption
      );

      await rootCommand.InvokeAsync(args);

      return exitCode;
    }
  }
}
