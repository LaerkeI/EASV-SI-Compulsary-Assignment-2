using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace TimelineService.Tests
{
    public class ComponentTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ComponentTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();

        }

        [Fact]
        public async Task GetTimeline_ShouldReturnOk()
        {
            // Arrange
            await _client.PostAsJsonAsync("api/users", new { Id = 1, Username = "johndoe", FollowedUsers = new int[] { 2 } });
            await _client.PostAsJsonAsync("api/users", new { Id = 2, Username = "janedoe", FollowedUsers = new int[] { } });
            await _client.PostAsJsonAsync("api/tweets", new { Id = 1, UserId = 2, Content = "Hello from Jane!" });

            // Act
            var response = await _client.GetAsync("/timeline/1");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
