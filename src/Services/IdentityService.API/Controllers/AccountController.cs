using Microsoft.OpenApi.Models;
using IdentityService.API.Domain.Models;
using IdentityService.API.Dtos;
using IdentityService.API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly SignInManager<ApplicationUser> _signInMgr;
        private readonly IConfiguration _config;

        public AccountController(
            UserManager<ApplicationUser>    userMgr,
            SignInManager<ApplicationUser>  signInMgr,
            IConfiguration                  config)
        {
            _userMgr    = userMgr;
            _signInMgr  = signInMgr;
            _config     = config;
        }

        // POST api/Account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser {
                UserName    = dto.Email,
                Email       = dto.Email,
                FirstName   = dto.FirstName,
                LastName    = dto.LastName,
                Address     = dto.Address,
                City        = dto.City,
                State       = dto.State,
                PostalCode  = dto.PostalCode,
                Country     = dto.Country,
                DateOfBirth = dto.DateOfBirth
            };

            var result = await _userMgr.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok();
        }

        // POST api/Account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userMgr.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();

            var loginResult = await _signInMgr.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!loginResult.Succeeded) return Unauthorized();

            // get JWT-token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!)
            };

            // add role claims
            var roles = await _userMgr.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer:    _config["Jwt:Issuer"],
                audience:  _config["Jwt:Audience"],
                claims:    claims,
                expires:   DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return Ok(new {
                token = new JwtSecurityTokenHandler().WriteToken(jwt)
            });
        }

        // GET api/Account/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Address,
                user.City,
                user.State,
                user.PostalCode,
                user.Country,
                user.DateOfBirth
            });
        }

        // PUT api/Account/me
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null) return NotFound();

            if (dto.FirstName   != null) user.FirstName   = dto.FirstName;
            if (dto.LastName    != null) user.LastName    = dto.LastName;
            if (dto.Address     != null) user.Address     = dto.Address;
            if (dto.City        != null) user.City        = dto.City;
            if (dto.State       != null) user.State       = dto.State;
            if (dto.PostalCode  != null) user.PostalCode  = dto.PostalCode;
            if (dto.Country     != null) user.Country     = dto.Country;
            if (dto.DateOfBirth != null) user.DateOfBirth = dto.DateOfBirth;

            var result = await _userMgr.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        // DELETE api/Account/me
        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userMgr.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
