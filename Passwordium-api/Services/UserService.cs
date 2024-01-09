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
        private readonly TokenService _tokenService;
        private readonly HashService _hashService;

        public UserService(DatabaseContext context, TokenService tokenService, HashService hashService)
        {
            _context = context;
            _tokenService = tokenService;
            _hashService = hashService;
        }

        public async Task<LoginResponse> LoginAsync(UserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username) ?? throw new Exception("User not found.");
            var passwordVerification = _hashService.VerifyHashedPassword(user, request.Password);
            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                throw new InvalidDataException("Incorrect password.");
            }

            string jwt = _tokenService.GenerateJwtToken(user);
            user = _tokenService.GenerateRefreshToken(user, _context);

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

            newUser = _hashService.HashPassword(newUser);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
        }

        public async Task<LoginResponse> TokenRefreshAsync(TokenRefreshRequest request, string jwtToken)
        {
            int userId = _tokenService.GetUserIdFromJWT(jwtToken);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (request.RefreshToken != user.RefreshToken || user.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exception("Invalid refresh token.");
            }

            string newJWT = _tokenService.GenerateJwtToken(user);
            user = _tokenService.GenerateRefreshToken(user, _context);

            LoginResponse loginResponse = new LoginResponse
            {
                JWT = newJWT,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiresAt = (DateTime)user.ExpiresAt
            };

            return loginResponse;
        }
    }
}
