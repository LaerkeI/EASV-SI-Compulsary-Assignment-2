namespace ApiGateway
{
    using System.Text;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;
    public class Program
    {
        public static async Task Main(string[] args)
        {
            const string SecurityKey = "MyClientSecretThatIsDefinitelyNotTooShort";

            var builder = WebApplication.CreateBuilder(args);

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
            
            // Add Ocelot to the services
            builder.Services.AddOcelot(builder.Configuration);
            
            var app = builder.Build();

            // Add Ocelot to application
            app.UseOcelot().Wait();

            app.UseAuthentication();

            app.UseAuthorization();
          
            app.Run();
        }
    }
}
