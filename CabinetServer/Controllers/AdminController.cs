using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CabinetServer.Data;
using CabinetServer.Data.Dtos;
using CabinetServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CabinetServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private IUserService _userService;
        private IMapper _mapper;

        public AdminController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpPost("Setup")]
        public async Task<IActionResult> Setup(ApplicationSetupDto dto)
        {
            if (_userService.GetUser("admin") != null)
                return BadRequest(new { error = "Appliance already set up." });

            ApplicationUser user = _mapper.Map<ApplicationUser>(dto.User);
            await _userService.CreateUser(user, dto.User.Password);
            return Ok();
        }
    }
}