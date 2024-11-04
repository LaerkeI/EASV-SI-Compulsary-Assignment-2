namespace PostManagementService
{
    using EasyNetQ;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add RabbitMQ bus to the services container
            builder.Services.AddSingleton<IBus>(_ =>
            {
                var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
                return RabbitHutch.CreateBus($"host={rabbitMqHost}");
            });

            var app = builder.Build();

            // In-memory data store for tweets
            var tweets = new List<Tweet>();

            // Post a new tweet
            app.MapPost("/tweets", (Tweet tweet) =>
            {
                tweets.Add(tweet);
                return Results.Ok(tweet);
            });

            // Get tweets by userId
            app.MapGet("/tweets", (int userId) =>
            {
                var userTweets = tweets.Where(t => t.UserId == userId).ToList();
                return Results.Ok(userTweets);
            });

            // Like a tweet (send RabbitMQ message to Notification Service)
            app.MapPost("/tweets/{id}/like", async (int id, LikeRequest request, IBus bus) =>
            {
                var tweet = tweets.FirstOrDefault(t => t.Id == id);

                if (tweet is null)
                    return Results.NotFound();

                // Log message publishing
                Console.WriteLine($"Publishing like event for Tweet {id} by user {request.UserId}");

                // Send message to Notification Service using RabbitMQ
                await bus.PubSub.PublishAsync(new TweetLikedMessage { TweetId = id, LikedByUserId = request.UserId });

                return Results.Ok();
            });

            app.Run();
        }

        public record Tweet
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string Content { get; set; }
        }

        public class TweetLikedMessage
        {
            public int TweetId { get; set; }
            public int LikedByUserId { get; set; }
        }

        public class LikeRequest
        {
            public int UserId { get; set; }
        }
    }
}
