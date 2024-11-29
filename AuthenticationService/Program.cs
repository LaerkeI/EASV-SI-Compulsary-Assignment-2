namespace AuthenticationService
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.AspNetCore.Builder;
    using Serilog;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("/var/log/app/authentication-service.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting AuthenticationService");

                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog to the logging pipeline
                builder.Host.UseSerilog();

                // Register health check services
                builder.Services.AddHealthChecks();

                builder.Services.AddAuthentication();
                builder.Services.AddAuthorization();

                var app = builder.Build();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapPost("/login", () =>
                {
                    var logger = Log.ForContext("Endpoint", "/login");
                    try
                    {
                        logger.Information("Login request received. Generating token...");

                        var token = CreateToken();

                        logger.Information("Token successfully generated");
                        return Results.Ok(token);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "An error occurred while generating the token");
                        return Results.StatusCode(500);
                    }
                });

                // Map the health check endpoint
                app.MapHealthChecks("/health");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "AuthenticationService terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        // Generate the token the user will use to sign in
        public static AuthenticationToken CreateToken()
        {
            const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShort";

            var logger = Log.ForContext("Method", nameof(CreateToken));
            try
            {
                logger.Information("Starting token generation");

                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey));
                var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

                // Claims describe what a user is allowed and not allowed to do
                var claims = new List<Claim>
                {
                    new Claim("scope", "/api/login.write"),
                    new Claim("scope", "/api/users.write"),
                    new Claim("scope", "/api/users/{id}.read"),
                    new Claim("scope", "/api/users/{id}/follow/{followedId}.write"),
                    new Claim("scope", "/api/tweets.write"),
                    new Claim("scope", "/api/tweets/{id}/like.write"),
                    new Claim("scope", "/api/timeline/{userId}.read"),
                    new Claim("scope", "/api/user-management-service/generate-token.write")
                };

                logger.Information("Claims created: {ClaimCount}", claims.Count);

                var tokenOptions = new JwtSecurityToken(
                    signingCredentials: signingCredentials,
                    claims: claims
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

                logger.Information("Token generation successful");

                return new AuthenticationToken { Value = tokenString };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred during token generation");
                throw;
            }
        }

        public class AuthenticationToken
        {
            public string Value { get; set; }
        }
    }
}
