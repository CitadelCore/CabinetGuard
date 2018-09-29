using CabinetServer.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CabinetServer.Services
{
    public interface IUserService
    {
        ApplicationUser Authenticate(string username, string password);
        IEnumerable<ApplicationUser> GetAllUsers();
        ApplicationUser GetUser(string username);
        Task<ApplicationUser> CreateUser(ApplicationUser user, string password);
        Task UpdateUser(ApplicationUser user, string newPassword = null);
        Task DeleteUser(string username);
    }
    public class UserService : IUserService
    {
        private ApplicationDbContext _context;
        private IPasswordHasher<ApplicationUser> _hasher = new PasswordHasher<ApplicationUser>();
        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Attempts to authenticate a user using their username and a provided password.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="providedPassword">The password the user provided.</param>
        /// <returns></returns>
        public ApplicationUser Authenticate(string username, string providedPassword)
        {
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(providedPassword))
                return null;

            ApplicationUser user = _context.Users.SingleOrDefault(u => u.UserName == username);
            if (user == null)
                return null;

            if (_hasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword) == PasswordVerificationResult.Failed)
                return null;

            return user;
        }

        public async Task<ApplicationUser> CreateUser(ApplicationUser user, string password)
        {
            if (_context.Users.Any(u => u.UserName == user.UserName))
                throw new Exception("Username is already present.");

            if (String.IsNullOrWhiteSpace(password))
                throw new Exception("Failed to validate user password.");

            user.PasswordHash = _hasher.HashPassword(user, password);
            _context.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task DeleteUser(string username)
        {
            ApplicationUser user = _context.Users.FirstOrDefault(u => u.UserName == username);
            if (user == null)
                throw new ArgumentException("The user does not exist.", "username");

            _context.Remove(user);
            await _context.SaveChangesAsync();
        }

        public IEnumerable<ApplicationUser> GetAllUsers()
        {
            return _context.Users;
        }

        public ApplicationUser GetUser(string username)
        {
            ApplicationUser user = _context.Users.FirstOrDefault(u => u.UserName == username);
            return user;
        }

        public async Task UpdateUser(ApplicationUser user, string newPassword = null)
        {
            if (!String.IsNullOrWhiteSpace(newPassword))
                user.PasswordHash = _hasher.HashPassword(user, newPassword);

            _context.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
