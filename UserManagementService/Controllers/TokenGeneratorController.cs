//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Text;

//namespace UserManagementService.Controllers
//{
//    [Route("api/user-management-service/[controller]")]
//    [ApiController]
//    public class TokenGeneratorController : ControllerBase
//    {
//        private string _jwtKey;
//        private string _jwtIssuer;

//        public TokenGeneratorController(string jwtKey, string jwtIssuer)
//        {
//            _jwtKey = jwtKey;
//            _jwtIssuer = jwtIssuer;
//        }

//        [HttpGet]
//        public IActionResult Get()
//        {
//            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
//            var credentials = new SigningCredentials(securityKey, algorithm: SecurityAlgorithms.HmacSha256);

//            var securityToken = new JwtSecurityToken(
//                _jwtIssuer,
//                _jwtIssuer,
//                null,
//                expires: DateTime.Now.AddHours(12),
//                signingCredentials: credentials
//            );

//            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
//            return Ok(token);
//        }
//    }
//}
