using bntuapplicants_backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace bntuapplicants_backend.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user, List<int> specialtyIds, List<int> facultyAccessIds)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
            };

            if (user.FacultyId.HasValue)
                claims.Add(new Claim("faculty_id", user.FacultyId.Value.ToString()));

            if (specialtyIds.Count != 0)
                claims.Add(new Claim("specialty_ids", string.Join(",", specialtyIds)));

            if (facultyAccessIds.Count != 0)
                claims.Add(new Claim("faculty_access_ids", string.Join(",", facultyAccessIds)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(int.Parse(_config["Jwt:ExpirationHours"] ?? "8"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
