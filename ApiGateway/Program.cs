namespace ApiGateway
{
    using System.Net;
    using System.Text;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;
    using Ocelot.Provider.Polly;
    using Polly;
    using Serilog;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("/var/log/app/api-gateway.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting ApiGateway");

                const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShort";

                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog to the logging pipeline
                builder.Host.UseSerilog();

                // Register health check services
                builder.Services.AddHealthChecks();

                // Load Ocelot configuration from the ocelot.json file
                builder.Configuration.AddJsonFile("ocelot.json", false, false);

                Log.Information("Loaded Ocelot configuration");

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(
                    JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey))
                        };
                    });

                Log.Information("Configured JWT Authentication");

                // Register IHttpClientFactory with Polly policies
                builder.Services.AddHttpClient("OcelotHttpClient")
                    .AddTransientHttpErrorPolicy(policyBuilder =>
                        policyBuilder.WaitAndRetryAsync(3, retryAttempt =>
                        {
                            Log.Warning("Transient error occurred. Retrying {RetryAttempt}...", retryAttempt);
                            return TimeSpan.FromMilliseconds(500);
                        }))
                    .AddTransientHttpErrorPolicy(policyBuilder =>
                        policyBuilder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
                    .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<HttpRequestException>()
                        .FallbackAsync(async cancellationToken =>
                        {
                            Log.Warning("Service is unavailable. Falling back to graceful degradation.");
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent("{\"message\": \"Service temporarily unavailable\"}",
                                    Encoding.UTF8, "application/json")
                            };
                        }));

                Log.Information("Configured HttpClient with Polly policies");

                // Add Ocelot to the services
                builder.Services.AddOcelot(builder.Configuration).AddPolly();

                var app = builder.Build();

                app.UseAuthentication();
                Log.Information("Authentication middleware configured");

                app.UseAuthorization();
                Log.Information("Authorization middleware configured");

                // Add Ocelot to application
                Log.Information("Starting Ocelot middleware");
                await app.UseOcelot();

                // Map the health check endpoint
                app.MapHealthChecks("/health");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ApiGateway terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
