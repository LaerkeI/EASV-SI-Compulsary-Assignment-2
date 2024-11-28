using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Claims;
using System.Text;

namespace UserManagementService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration.GetSection("Settings").Get<Settings>();

            string jwtIssuer = config.JwtIssuer;
            string jwtKey = config.JwtKey;
            string userManagementServiceToken = config.UserManagementServiceToken;


            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtIssuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    };
                }
            );

            builder.Services.AddAuthorization();

            builder.Services.AddSingleton(jwtKey);
            builder.Services.AddSingleton(jwtIssuer);
            // Register the token for use in HTTP requests
            builder.Services.AddSingleton(userManagementServiceToken);

            // Add HttpClient and configure it with Authorization header
            builder.Services.AddHttpClient("UserManagementServiceClient", client =>
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userManagementServiceToken);
            });


            // Register health check services
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // In-memory data store
            var users = new List<User> 
            {
                new User { Id = 1, Username = "john_doe", FollowedUsers = new List<int>() }
            };

            app.MapPost("/users", [Authorize] (User user) =>
            {
                users.Add(user);
                return Results.Ok(user);
            });

            app.MapGet("/users", [Authorize] () =>
            {
                return Results.Ok(users);

            });

            app.MapGet("/users/{id}", [Authorize] (int id) => 
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            app.MapPost("/users/{id}/follow/{followedId}", [Authorize] (int id, int followedId) => 
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                var followedUser = users.FirstOrDefault(u => u.Id == followedId);

                if (user is null || followedUser is null)
                    return Results.NotFound();

                user.FollowedUsers.Add(followedId);
                return Results.Ok();
            });

            // Generate Token for East West security
            app.MapPost("/user-management-service/generate-token", () =>
            {
                var token = CreateToken();
                return Results.Ok(token);
            });

            app.UseAuthentication();
            app.UseAuthorization();

            // Map the health check endpoint
            app.MapHealthChecks("/health");

            app.Run();
        }
        private static AuthenticationToken CreateToken()
        {
            const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShortUserManagementService";

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            // Claims can contain user roles and permissions or scopes
            var claims = new List<Claim>
            {
                new Claim("scope", "/api/users.write"),
                new Claim("scope", "/api/users.write"),
                new Claim("scope", "/api/users/{id}.read"),
                new Claim("scope", "/api/users/{id}/follow/{followedId}.write")
            };

            var tokenOptions = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims,
                expires: DateTime.Now.AddHours(1)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            return new AuthenticationToken { Value = tokenString };
        }
        public record User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public List<int> FollowedUsers { get; set; }
        }
    }
}
