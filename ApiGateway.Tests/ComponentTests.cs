namespace ApiGateway.Tests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.AspNetCore.Mvc.Testing;
    using System.Net;
    using Newtonsoft.Json.Linq;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public class ComponentTests : IClassFixture<ComponentTests.CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ComponentTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseUrls("http://api-gateway:80"); // Ensure it matches your Ocelot configuration
            }
        }

        [Fact]
        public async Task GenerateToken_ShouldReturnOkWithToken()
        {
            // Act
            var response = await _client.GetAsync("/generate-token");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(jsonString);
            Assert.NotNull(jsonResponse["token"]);
        }

        [Fact]
        public async Task AccessSecuredEndpoint_UnauthorizedWithoutToken()
        {
            // Act
            var response = await _client.GetAsync("/secured-endpoint");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccessSecuredEndpoint_WithValidToken_ShouldAuthorize()
        {
            // Generate a token first
            var tokenResponse = await _client.GetAsync("/generate-token");
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

            // Log the token response for debugging
            System.Diagnostics.Debug.WriteLine($"Token Response: {tokenContent}");

            var jsonResponse = JObject.Parse(tokenContent);
            var token = jsonResponse["token"].ToString();

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/secured-endpoint");

            // Log the response content for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Secured Endpoint Response: {responseContent}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("You are authorized!", responseContent);
        }
    }
}
