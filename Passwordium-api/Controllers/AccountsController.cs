﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Passwordium_api.Data;
using Passwordium_api.Model.Entities;
using Passwordium_api.Model.Requests;
using Passwordium_api.Model.Responses;
using Passwordium_api.Services;

namespace Passwordium_api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly TokenService _tokenService;

        public AccountsController(DatabaseContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<List<Account>>> GetAccounts()
        {
            string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            int userId = _tokenService.GetUserIdFromJWT(jwt);

            var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();

            if (accounts == null)
            {
                return NotFound(new { message = "Accounts does not exists." });
            }

            return accounts;
        }

        // PUT: api/Accounts
        [HttpPut]
        public async Task<IActionResult> PutAccount(AccountRequest account)
        {
            string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            int userId = _tokenService.GetUserIdFromJWT(jwt);

            var accountReal = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
            if (accountReal == null)
            {
                return NotFound(new { message = "Account does not exists." });
            }
            if (accountReal.UserId != userId)
            {
                return BadRequest(new { message = "That account is not yours." });
            }

            accountReal.Name = account.Name;
            accountReal.Url = account.Url;
            accountReal.Username = account.Username;
            accountReal.Password = account.Password;

            _context.Entry(accountReal).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound(new { message = "Did not update account." });
            }

            return Ok(new { message = "Account updated!" });
        }

        // POST: api/Accounts
        [HttpPost]
        public async Task<ActionResult<Account>> PostAccount(AccountRequest account)
        {
            string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            int userId = _tokenService.GetUserIdFromJWT(jwt);

            Account newAccount = new Account
            {
                Name = account.Name,
                Url = account.Url,
                Username = account.Username,
                Password = account.Password,
                UserId = userId,
                User = await _context.Users.FirstAsync(u => u.Id == userId)
            };
            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account added to database!" });
        }

        // DELETE: api/Accounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            string jwt = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            int userId = _tokenService.GetUserIdFromJWT(jwt);

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound(new { message = "Account does not exists." });
            }
            if (userId != account.UserId)
            {
                return BadRequest(new { message = "That account is not yours." });
            }

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account deleted from database!" });
        }

        // POST: api/Accounts/CheckPasswords
        [HttpPost("CheckPasswords")]
        public async Task<ActionResult<List<CheckPasswordsRequest>>> CheckPasswords(List<CheckPasswordsRequest> request)
        {
            string apiURL = "https://api.pwnedpasswords.com/range/";
            HttpClient client = new HttpClient();
            List<CheckPasswordsResponse> response = new List<CheckPasswordsResponse>();
            
            foreach (CheckPasswordsRequest passRequest in request)
            {
                string prefix = passRequest.Password.Substring(0, 5);
                string suffix = passRequest.Password.Substring(5);


                HttpResponseMessage apiResponse = await client.GetAsync(apiURL + prefix);
                string apiResponseString = await apiResponse.Content.ReadAsStringAsync();
                bool isPasswordBreached = apiResponseString.Contains(suffix.ToUpper());

                if (isPasswordBreached)
                {
                    response.Add(new CheckPasswordsResponse { Id = passRequest.Id });
                }
            }

            return Ok(response);
        }
    }
}
