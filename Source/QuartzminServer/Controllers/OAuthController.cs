using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;

using IdentityModel;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

namespace Quartzmin.Controllers
{
    using static QuartzminHelper;

    public class UserDto
    {
        public string UserName;
        public string Password;
    }

    public class OAuthController : PageControllerBase
    {
        private static IEnumerable<UserDto> GetUsers()
        {
            var file = Path.Combine(PwdPath, "data", "users.json");
            if (System.IO.File.Exists(file))
            {
                return JsonConvert.DeserializeObject<UserDto[]>(System.IO.File.ReadAllText(file));
            }
            return Array.Empty<UserDto>();
        }

        [HttpPost]
        public IActionResult Authenticate([FromBody] UserDto userDto)
        {
            var users = GetUsers();
            if (!users.Any(n=>n.UserName == userDto.UserName && n.Password == userDto.Password))
            {
                return Unauthorized();
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(OAuthSecret);
            var authTime = DateTime.UtcNow;
            var expiresAt = authTime.AddDays(1);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtClaimTypes.Audience,OAuthAudience),
                    new Claim(JwtClaimTypes.Issuer,OAuthIssuer),
                    new Claim(JwtClaimTypes.Id, userDto.UserName),
                    new Claim(JwtClaimTypes.Name, userDto.UserName)
                }),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            Response.Cookies.Append(OAuthCookieName, tokenString);
            return Ok(new
            {
                access_token = tokenString,
                token_type = "Bearer",
                profile = new {
                    auth_time = new DateTimeOffset(authTime).ToUnixTimeSeconds(),
                    expires_at = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
                }
            });
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(null);
        }
    }
}