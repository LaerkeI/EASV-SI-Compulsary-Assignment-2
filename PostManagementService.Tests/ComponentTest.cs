namespace PostManagementService.Tests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;
    using PostManagementService;
    using System.Net.Http.Json;

    public class ComponentTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ComponentTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostTweet_ShouldReturnOk()
        {
            // Arrange
            var tweet = new { Id = 1, UserId = 1, Content = "Hello World" };

            // Act
            var response = await _client.PostAsJsonAsync("/tweets", tweet);

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}

