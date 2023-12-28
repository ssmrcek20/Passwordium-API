using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Passwordium_api.Data;
using Passwordium_api.Model.Entities;
using Passwordium_api.Model.Requests;
using Passwordium_api.Model.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Passwordium_api.Services
{
    public class UserService
    {
        private readonly DatabaseContext _context;
        private readonly IConfiguration _config;

        public UserService(DatabaseContext context, IConfiguration configuration)
        {
            _context = context;
            _config = configuration;
        }

        public async Task<LoginResponse> LoginAsync(UserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            HashService hashService = new HashService();
            var passwordVerification = hashService.VerifyHashedPassword(user, request.Password);
            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                throw new InvalidDataException("Incorrect password.");
            }

            TokenService tokenService = new TokenService();
            string jwt = tokenService.GenerateJwtToken(user, _config);
            user = tokenService.GenerateRefreshToken(user, _context);

            LoginResponse response = new LoginResponse
            {
                JWT = jwt,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiresAt = (DateTime)user.ExpiresAt
            };

            return response;
        }

        public async Task RegisterAsync(UserRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
            {
                throw new InvalidDataException("A user with this username already exists.");
            }

            User newUser = new User
            {
                Username = request.Username,
                Password = request.Password
            };

            HashService hashService = new HashService();
            newUser = hashService.HashPassword(newUser);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
        }
    }
}
