using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace UserManagementService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("/var/log/app/user-management-service.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting UserManagementService");

                var builder = WebApplication.CreateBuilder(args);
                var config = builder.Configuration.GetSection("Settings").Get<Settings>();

                string jwtIssuer = config.JwtIssuer;
                string jwtKey = config.JwtKey;
                string userManagementServiceToken = config.UserManagementServiceToken;

                // Add Serilog to the logging pipeline
                builder.Host.UseSerilog();

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
                    });

                Log.Information("Configured JWT Authentication");

                builder.Services.AddAuthorization();

                builder.Services.AddSingleton(jwtKey);
                builder.Services.AddSingleton(jwtIssuer);
                builder.Services.AddSingleton(userManagementServiceToken);

                // Add HttpClient with Authorization header
                builder.Services.AddHttpClient("UserManagementServiceClient", client =>
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userManagementServiceToken);
                });

                Log.Information("Registered HttpClient with Authorization header");

                // Register health check services
                builder.Services.AddHealthChecks();

                var app = builder.Build();

                // In-memory data store
                var users = new List<User>
                {
                    new User { Id = 1, Username = "john_doe", FollowedUsers = new List<int>() }
                };

                // Define API endpoints
                app.MapPost("/users", [Authorize] (User user) =>
                {
                    Log.Information("Adding new user: {@User}", user);
                    users.Add(user);
                    return Results.Ok(user);
                });

                app.MapGet("/users", [Authorize] () =>
                {
                    Log.Information("Fetching all users");
                    return Results.Ok(users);
                });

                app.MapGet("/users/{id}", [Authorize] (int id) =>
                {
                    Log.Information("Fetching user by ID: {UserId}", id);
                    var user = users.FirstOrDefault(u => u.Id == id);
                    return user is not null ? Results.Ok(user) : Results.NotFound();
                });

                app.MapPost("/users/{id}/follow/{followedId}", [Authorize] (int id, int followedId) =>
                {
                    Log.Information("User {UserId} following User {FollowedId}", id, followedId);
                    var user = users.FirstOrDefault(u => u.Id == id);
                    var followedUser = users.FirstOrDefault(u => u.Id == followedId);

                    if (user is null || followedUser is null)
                    {
                        Log.Warning("User {UserId} or FollowedUser {FollowedId} not found", id, followedId);
                        return Results.NotFound();
                    }

                    user.FollowedUsers.Add(followedId);
                    return Results.Ok();
                });

                app.MapPost("/user-management-service/generate-token", () =>
                {
                    Log.Information("Generating new authentication token");
                    var token = CreateToken();
                    return Results.Ok(token);
                });

                app.UseAuthentication();
                Log.Information("Authentication middleware configured");

                app.UseAuthorization();
                Log.Information("Authorization middleware configured");

                // Map the health check endpoint
                app.MapHealthChecks("/health");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "UserManagementService terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static AuthenticationToken CreateToken()
        {
            const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShort";

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            // Claims can contain user roles and permissions or scopes
            var claims = new List<Claim>
            {
                new Claim("scope", "/api/users.write"),
                new Claim("scope", "/api/users/{id}.read"),
                new Claim("scope", "/api/users/{id}/follow/{followedId}.write")
            };

            var tokenOptions = new JwtSecurityToken(
                "http://user-management-service",
                "http://user-management-service",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: signingCredentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            var authToken = new AuthenticationToken { Value = tokenString };

            Log.Information("Generated authentication token: {Token}", tokenString);

            return authToken;
        }

        public record User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public List<int> FollowedUsers { get; set; }
        }
    }
}
