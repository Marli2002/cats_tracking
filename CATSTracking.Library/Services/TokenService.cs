using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CATSTracking.Library.Services
{
    public static class TokenService
    {

        //TODO : Refactor
        public static Models.Token NewToken(IdentityUser user, UserManager<IdentityUser> userManager)
        {
            var userRoles = userManager.GetRolesAsync(user).Result;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("gXhKRMAOyBdr3RG44XM8eMSqgKl1JQQBj4XMhe0ec="));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "CATSAPI",
                audience: "CATSAPI",
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: creds);



            return new Models.Token
            {
                JWT = new JwtSecurityTokenHandler().WriteToken(token),
                Expires = token.ValidTo,
                UserName = user.UserName,
                Grants = string.Join(",", userRoles)
            };
        }
        
    }
}
