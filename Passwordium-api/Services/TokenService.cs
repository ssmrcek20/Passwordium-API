using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Passwordium_api.Data;
using Passwordium_api.Model.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Passwordium_api.Services
{
    public class TokenService
    {
        public string GenerateJwtToken(User user, IConfiguration config)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(config.GetSection("JWT:Key").Value);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("id",user.Id.ToString()),
                    new Claim(ClaimTypes.Name,user.Username)
                }),
                Expires = DateTime.UtcNow.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                               SecurityAlgorithms.HmacSha256Signature),
                Audience = config.GetSection("JWT:Audience").Value,
                Issuer = config.GetSection("JWT:Issuer").Value
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public User GenerateRefreshToken(User user, DatabaseContext context)
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                user.RefreshToken = Convert.ToBase64String(randomNumber);
            }
            user.ExpiresAt = DateTime.UtcNow.AddHours(2);

            context.Users.Update(user);
            context.SaveChanges();

            return user;
        }
    }
}
