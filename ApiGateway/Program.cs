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
    public class Program
    {
        public static async Task Main(string[] args)
        {
            const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShort";

            var builder = WebApplication.CreateBuilder(args);

            // Register health check services
            builder.Services.AddHealthChecks();

            // Load Ocelot configuration from the ocelot.json file
            builder.Configuration.AddJsonFile("ocelot.json", false, false);

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
                }  
            );

            // Register IHttpClientFactory with Polly policies
            builder.Services.AddHttpClient("OcelotHttpClient")
                .AddTransientHttpErrorPolicy(policyBuilder => 
                    policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500)))
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
                .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<HttpRequestException>()
                    .FallbackAsync(async cancellationToken =>
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK) //The status code 200 is used for graceful degradation
                        {
                            Content = new StringContent("{\"message\": \"Service temporarily unavailable\"}",
                                Encoding.UTF8, "application/json")
                        };
                    }));

            // Add Ocelot to the services
            builder.Services.AddOcelot(builder.Configuration).AddPolly();

            var app = builder.Build();

            app.UseAuthentication();

            app.UseAuthorization();
            
            // Add Ocelot to application
            app.UseOcelot().Wait();

            // Map the health check endpoint
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
