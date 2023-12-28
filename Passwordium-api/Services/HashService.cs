using Microsoft.AspNetCore.Identity;
using Passwordium_api.Model.Entities;

namespace Passwordium_api.Services
{
    public class HashService
    {
        public User HashPassword(User newUser)
        {
            var passwordHasher = new PasswordHasher<User>();
            string hashedPassword = passwordHasher.HashPassword(newUser, newUser.Password);
            newUser.Password = hashedPassword;
            return newUser;
        }

        public PasswordVerificationResult VerifyHashedPassword(User user, string password)
        {
            var passwordHasher = new PasswordHasher<User>();
            return passwordHasher.VerifyHashedPassword(user, user.Password, password);
        }
    }
}
