using CsInstall;
using System;
using System.Security.Cryptography.X509Certificates;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Installs the certificate.
  /// </summary>
  internal class InstallCertificate : IInstallerTask {
    private X509Store? x509Store;
    private X509Certificate2? certificate;

    public bool SkipInstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="InstallCertificate"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal InstallCertificate(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    /// <summary>
    /// Checks whether the certificate is already installed.
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Checks() {
      try {
        x509Store = new(StoreName.Root, StoreLocation.LocalMachine);
        x509Store.Open(OpenFlags.ReadWrite);

        certificate = new(
          X509Certificate2.CreateFromCertFile(
            Definition.Installer.File(GenericShellExInfrastructure.CertificateFile)
          )
        );

        if (x509Store.Certificates.Contains(certificate)) {
          x509Store.Close();
          SkipInstall = true;

          Definition.Installer.Log("Certificate already installed, won't install again");
        }
      } catch (Exception e) {
        throw new InstallerException($"Unable to check if certificate {GenericShellExInfrastructure.CertificateFile} is already installed.", e: e);
      }
    }

    /// <summary>
    /// <inheritdoc cref="InstallCertificate"/>
    /// </summary>
    /// <exception cref="InstallerException"></exception>
    public void Install() {
      try {
        x509Store!.Add(certificate!);
        x509Store.Close();

        Definition.Installer.Log($"Installed certificate {GenericShellExInfrastructure.CertificateFile}");
      } catch (Exception e) {
        throw new InstallerException($"Unable to install certificate {GenericShellExInfrastructure.CertificateFile}.", e: e);
      }
    }
  }
}