namespace PostManagementService.Tests
{ 
    using PostManagementService;
    using Xunit;
    using EasyNetQ;
    using Moq;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class UnitTests
    {
        [Fact]
        public void AddTweet_ShouldAddTweetToList()
        {
            // Arrange
            var tweets = new List<Program.Tweet>();
            var newTweet = new Program.Tweet { Id = 1, UserId = 1, Content = "Hello World" };

            // Act
            tweets.Add(newTweet);

            // Assert
            Assert.Contains(newTweet, tweets);
        }

        //[Fact]
        //public async Task LikeTweet_ShouldSendMessageToRabbitMQ()
        //{
        //    // Arrange
        //    var mockBus = new Mock<IBus>(); // Mocking the EasyNetQ IBus interface
        //    var mockPubSub = new Mock<IPubSub>();
        //    mockBus.Setup(b => b.PubSub).Returns(mockPubSub.Object); // Set up PubSub to return our mock

        //    var tweets = new List<Program.Tweet>
        //    {
        //        new Program.Tweet { Id = 1, UserId = 1, Content = "Hello World" }
        //    };

        //    var tweetId = 1;
        //    var userId = 2;
        //    var tweet = tweets.FirstOrDefault(t => t.Id == tweetId);

        //    // Setup: Expect the PublishAsync method to be called with any TweetLikedMessage
        //    mockPubSub.Setup(x => x.PublishAsync(It.IsAny<Program.TweetLikedMessage>(), default)).Returns(Task.CompletedTask);

        //    // Act
        //    if (tweet != null)
        //    {
        //        var likedMessage = new Program.TweetLikedMessage { TweetId = tweetId, LikedByUserId = userId };
        //        await mockBus.Object.PubSub.PublishAsync(likedMessage);  // Simulate publishing message to RabbitMQ
        //    }

        //    // Assert
        //    mockPubSub.Verify(x => x.PublishAsync(It.IsAny<Program.TweetLikedMessage>(), default), Times.Once); // Verify publish was called once
        //}

    }
}
