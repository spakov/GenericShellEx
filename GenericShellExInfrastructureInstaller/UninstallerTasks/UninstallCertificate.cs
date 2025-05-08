using CsInstall;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System;

#nullable enable
namespace GenericShellExInfrastructureInstaller {
  /// <summary>
  /// Uninstalls the certificate.
  /// </summary>
  internal class UninstallCertificate : IUninstallerTask {
    public bool SkipUninstall { get; set; } = false;

    /// <summary>
    /// The <see cref="GenericShellExInfrastructure"/> that corresponds to this
    /// <see cref="IInstallerTask"/>.
    /// </summary>
    private GenericShellExInfrastructure Definition { get; set; }

    /// <summary>
    /// Initializes <see cref="UninstallCertificate"/>.
    /// </summary>
    /// <param name="definition"><inheritdoc cref="Definition"
    /// path="/summary"/></param>
    internal UninstallCertificate(GenericShellExInfrastructure definition) {
      Definition = definition;
    }

    public void Checks() { }

    /// <summary>
    /// <inheritdoc cref="UninstallCertificate"/>
    /// </summary>
    /// <exception cref="UninstallerException"></exception>
    public void Uninstall() {
      try {
        X509Store x509Store = new(StoreName.TrustedPeople, StoreLocation.LocalMachine);
        x509Store.Open(OpenFlags.ReadWrite);

        List<X509Certificate2> certificatesToRemove = new();

        foreach (X509Certificate2 certificate in x509Store.Certificates) {
          if (GenericShellExInfrastructure.KnownCertificateThumbprints.Contains(certificate.Thumbprint.ToLower())) {
            certificatesToRemove.Add(certificate);
          }
        }

        foreach (X509Certificate2 certificate in certificatesToRemove) {
          Definition.Installer.Log($"Removing certificate with thumbprint {certificate.Thumbprint}");
          x509Store.Remove(certificate);
        }

        x509Store.Close();
      } catch (Exception e) {
        throw new UninstallerException($"Unable to uninstall certificate.", e: e);
      }

      Definition.Installer.Log($"Uninstalled certificate");
    }
  }
}