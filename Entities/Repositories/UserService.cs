using Core.Interface;
using Identity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public class UserService : IUserService
    {
        public IConfiguration Configuration { get; }

        public UserService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private List<User> _users = new List<User>
        {
            new User { Id = 5, UserName = "Sinan", Password = "deneme123", Role = "admin"},
            new User { Id = 11, UserName = "Mikro", Password = "test123", Role = "guest"}
        };

        public string Login(string userName, string password)
        {
            var user = _users.SingleOrDefault(x => x.UserName == userName && x.Password == password);

            if (user == null)
            {
                return string.Empty;
            }

            var SecretKey = Configuration["SecretKey"];

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, user.Role)
                }),

                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            return user.Token;
        }
    }
}
