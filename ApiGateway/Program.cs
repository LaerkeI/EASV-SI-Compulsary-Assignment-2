namespace ApiGateway
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.IdentityModel.Tokens;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,  // Skipping issuer validation for simplicity
                        ValidateAudience = false,  // Skipping audience validation for simplicity
                        ValidateLifetime = true,  // Ensure the token hasn’t expired
                        ValidateIssuerSigningKey = true,  // Validate the token signature
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
                    };
                });

            // Add Ocelot to the services
            builder.Services.AddOcelot();

            // Load Ocelot configuration from the ocelot.json file
            builder.Configuration.AddJsonFile("ocelot.json");

            var app = builder.Build();

            // Enable Authentication and Authorization Middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Use Ocelot middleware for routing
            await app.UseOcelot();

            // This secured endpoint is for component tests.
            app.MapGet("/secured-endpoint", [Authorize] () => Results.Ok("You are authorized!"));

            app.MapGet("/generate-token", (IConfiguration config) => {
                var token = GenerateJwtToken(0, "admin", config);
                return Results.Ok(new { Token = token });
            });

            app.Run();
        }

        // Method to generate JWT token
        public static string GenerateJwtToken(int userid, string username, IConfiguration configuration)
        {
            // Retrieve JWT settings from configuration
            var secretKey = configuration["JwtSettings:SecretKey"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userid.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Iat, ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString()) // Current time as Unix timestamp
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(525600),  // Token is valid for 1 year
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
