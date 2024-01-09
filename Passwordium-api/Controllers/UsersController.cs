using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Elfie.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NuGet.Protocol.Plugins;
using Passwordium_api.Data;
using Passwordium_api.Model.Entities;
using Passwordium_api.Model.Requests;
using Passwordium_api.Model.Responses;
using Passwordium_api.Services;

namespace Passwordium_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly DatabaseContext _context;
        private readonly TokenService _tokenService;
        private readonly HashService _hashService;

        public UsersController(UserService userService, DatabaseContext context, TokenService tokenService, HashService hashService)
        {
            _userService = userService;
            _context=context;
            _tokenService = tokenService;
            _hashService=hashService;
        }

        // POST: api/Users/Login
        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponse>> Login(UserRequest request)
        {
            try
            {
                LoginResponse response = await _userService.LoginAsync(request);

                return Ok(response);
            }
            catch (InvalidDataException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserRequest request)
        {
            try
            {
                await _userService.RegisterAsync(request);

                return Ok(new { message = "User added to database!" });
            }
            catch (InvalidDataException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/Users/TokenRefresh
        [Authorize(AuthenticationSchemes = "NoExpiryCheck")]
        [HttpPost("TokenRefresh")]
        public async Task<ActionResult<LoginResponse>> TokenRefresh(TokenRefreshRequest request)
        {
            try
            {
                string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                LoginResponse response = await _userService.TokenRefreshAsync(request, jwt);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

        }

        // POST: api/Users/PublicKey
        [Authorize]
        [HttpPost("PublicKey")]
        public async Task<IActionResult> PublicKey(PublicKeyRequest request)
        {
            string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            int userId = _tokenService.GetUserIdFromJWT(jwt);

            var userReal = await _context.Users.FirstOrDefaultAsync(a => a.Id == userId);
            if (userReal == null)
            {
                return NotFound(new { message = "User does not exists." });
            }

            userReal.PublicKey = request.PublicKey;

            _context.Entry(userReal).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound(new { message = "Did not add public key." });
            }

            return Ok(new { message = "Public key stored!" });
        }

        // GET: api/Users/Challenge
        [HttpGet("Challenge")]
        public async Task<IActionResult> challenge()
        {
            string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            string userName = token.Claims.First(claim => claim.Type == "unique_name").Value;

            string hash = _hashService.GetHash(userName);
            return Ok(new { message = hash });
        }
    }
}
