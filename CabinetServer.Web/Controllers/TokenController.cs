using CabinetServer.Web;
using CorePlatform.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CabinetServer.Controllers
{
    /**
    [Route("api/[controller]")]
    public class TokenController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            // Check whether the client is requesting certificate authentication
            X509Certificate2 certificate = await HttpContext.Connection.GetClientCertificateAsync();

            if (certificate != null)
                return AuthViaCertificateAsync(certificate);

            return new UnauthorizedResult();
        }

        private IActionResult AuthViaCertificateAsync(X509Certificate2 certificate)
        {
            // Verify we have a machine certificate,
            // and that the certificate is valid and with correct extensions
            if (!certificate.Verify() ||
                !EncryptionUtilities.CertificateContainsOid(certificate, new Oid("1.3.6.1.5.5.7.3.2")) ||
                !EncryptionUtilities.CertificateIsTemplate(certificate, "Machine"))
                return new UnauthorizedResult();

            string clientDns = certificate.GetNameInfo(X509NameType.DnsName, false);

            Claim[] claims = new Claim[]
            {
                new Claim("AuthorizationType", "ControllerOnlyCertificate"),
                new Claim(ClaimTypes.Dns, clientDns),
                new Claim(ClaimTypes.SerialNumber, certificate.GetSerialNumberString()),
            };

            SigningCredentials credentials = new SigningCredentials(new X509SecurityKey(Startup.GetMachineCertificate()), SecurityAlgorithms.RsaSha256);
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: "tower.local",
                audience: "tower.local",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new OkObjectResult(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }*/
}
