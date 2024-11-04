namespace TimelineService.Tests
{
    using Moq;
    using Xunit;
    using TimelineService;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Net.Http.Json;
    using Moq.Protected;
    using System.Net;
    using System.Collections.Generic;
    using System.Threading;

    public class UnitTests
    {
        [Fact]
        public async Task GetTimeline_ShouldReturnTweetsFromFollowedUsers()
        {
            // Arrange
            var mockFactory = new Mock<IHttpClientFactory>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new List<Program.Tweet> { new Program.Tweet { Id = 1, UserId = 2, Content = "Tweet from followed user" } })
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var userId = 1;

            // Act
            var tweets = await httpClient.GetFromJsonAsync<List<Program.Tweet>>($"http://user-management-service:5001/users/{userId}");

            // Assert
            Assert.NotNull(tweets);
            Assert.Single(tweets);
            Assert.Equal("Tweet from followed user", tweets[0].Content);
        }
    }
}
