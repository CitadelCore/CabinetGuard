using CabinetServer.Data;
using CabinetServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoMapper;
using CabinetServer.Data.Dtos;

namespace CabinetServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;
        private IMapper _mapper;

        public UsersController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpPost("Authenticate")]
        public async Task<IActionResult> Authenticate(ApplicationUserDto dto)
        {
            // Check and see if we're using a client certificate here
            X509Certificate2 certificate = await HttpContext.Connection.GetClientCertificateAsync();
            if (certificate != null)
                throw new Exception("Certificate authentication not implemented.");

            ApplicationUser user = _userService.Authenticate(dto.Username, dto.Password);

            if (user == null)
                return BadRequest(new { error = "Authentication failed." });

            Claim[] claims = new Claim[]
            {
                new Claim("AuthorizationType", "User"),
                new Claim(ClaimTypes.Name, user.Id),
            };

            SigningCredentials credentials = new SigningCredentials(new X509SecurityKey(Startup.GetMachineCertificate()), SecurityAlgorithms.RsaSha256);
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: "tower.local",
                audience: "tower.local",
                claims: claims,
                expires: DateTime.Now.AddDays(2),
                signingCredentials: credentials);
            string returnToken = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                Id = user.Id,
                Username = user.UserName,
                Token = returnToken,
            });
        }

        [HttpGet("{username}")]
        public IActionResult Get(string username)
        {
            ApplicationUser user = _userService.GetUser(username);
            ApplicationUserDto dto = _mapper.Map<ApplicationUserDto>(user);
            return Ok(dto);
        }

        [HttpPut("{username}")]
        public IActionResult Put(string username, [FromBody] ApplicationUserDto dto)
        {
            ApplicationUser user = _mapper.Map<ApplicationUser>(dto);
            user.Id = username;

            _userService.UpdateUser(user, dto.Password);
            return Ok();
        }

        // TODO: Authorize to only be able to delete if you are the user
        [HttpDelete("{username}")]
        public IActionResult Delete(string username)
        {
            _userService.DeleteUser(username);
            return Ok();
        }
    }
}
