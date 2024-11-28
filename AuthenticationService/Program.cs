namespace AuthenticationService
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.AspNetCore.Builder;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register health check services
            builder.Services.AddHealthChecks();

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/login", () => {
                var token = CreateToken();
                return Results.Ok(token);
            });

            // Map the health check endpoint
            app.MapHealthChecks("/health");

            app.Run();
        }

        //Generate the token the user will use to sign in
        public static AuthenticationToken CreateToken()
        {
            //The secret key that is used to generate tokens
            const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShort";
            
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            //Claims describe what a user is allowed and not allowed to do
            var claims = new List<Claim>
            {
                new Claim("scope", "/api/login.write"),
                new Claim("scope", "/api/users.write"),
                new Claim("scope", "/api/users.write"),
                new Claim("scope", "/api/users/{id}.read"),
                new Claim("scope", "/api/users/{id}/follow/{followedId}.write"),
                new Claim("scope", "/api/tweets.write"),
                new Claim("scope", "/api/tweets/{id}/like.write"),
                new Claim("scope", "/api/timeline/{userId}.read"),
                new Claim("scope", "/api/user-management-service/generate-token.write")
            };

            var tokenOptions = new JwtSecurityToken(
                signingCredentials: signingCredentials, claims: claims
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions); 
            var authToken = new AuthenticationToken
            {
                Value = tokenString
            };
            
            return authToken;
        }   
    }
}
