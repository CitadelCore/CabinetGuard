using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CorePlatform.Utilities
{
    public class CertificateUtilities
    {
        public static OidCollection GetCertificateEkus(X509Certificate2 cert)
        {
            foreach (X509Extension extension in cert.Extensions)
            {
                if (extension.GetType() == typeof(X509EnhancedKeyUsageExtension))
                {
                    X509EnhancedKeyUsageExtension eku = (X509EnhancedKeyUsageExtension) extension;
                    return eku.EnhancedKeyUsages;
                }
            }

            return null;
        }

        public static bool CertificateContainsOid(X509Certificate2 cert, Oid oidToCompare)
        {
            foreach (Oid oid in GetCertificateEkus(cert))
            {
                if (oid.Value == oidToCompare.Value)
                    return true;
            }

            return false;
        }

        public static bool CertificateIsTemplate(X509Certificate2 cert, string templateName)
        {
            foreach (X509Extension extension in cert.Extensions)
            {
                if (extension.Oid.FriendlyName == "Certificate Template Name" && extension.Format(false) == templateName)
                    return true;
            }

            return false;
        }
    }
}
