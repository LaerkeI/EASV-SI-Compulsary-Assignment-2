namespace UserManagementService.Tests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;
    using UserManagementService;
    using System.Net.Http.Json;

    public class ComponentTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ComponentTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetUser_ShouldReturnUser_WhenUserExists()
        {
            // Act
            var response = await _client.GetAsync("/users/1");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("john_doe", responseContent);  // Check if the response contains the expected user data
        }

        [Fact]
        public async Task GetUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync("/users/999");  // User ID that doesn't exist

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);  // Ensure 404 Not Found
        }

        [Fact]
        public async Task AddUser_ShouldReturnOk_WhenValidUserIsAdded()
        {
            // Arrange
            var newUser = new
            {
                Id = 2,
                Username = "jane_doe",
                FollowedUsers = new int[] { }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/users", newUser);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task FollowUser_ShouldReturnOk_WhenValidFollowRequestIsMade()
        {
            // Act
            var response = await _client.PostAsync("/users/1/follow/2", null);  // User 1 follows User 2

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}


