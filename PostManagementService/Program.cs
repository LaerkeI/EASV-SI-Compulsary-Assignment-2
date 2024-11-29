namespace PostManagementService
{
    using EasyNetQ;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;

    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("/var/log/app/post-management-service.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting PostManagementService");

                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog to the logging pipeline
                builder.Host.UseSerilog();

                // Register health check services
                builder.Services.AddHealthChecks();

                // Add RabbitMQ bus to the services container
                builder.Services.AddSingleton<IBus>(_ =>
                {
                    var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
                    Log.Information("Configuring RabbitMQ with host: {RabbitMqHost}", rabbitMqHost);
                    return RabbitHutch.CreateBus($"host={rabbitMqHost}");
                });

                var app = builder.Build();

                // In-memory data store for tweets
                var tweets = new List<Tweet>();

                // Post a new tweet
                app.MapPost("/tweets", (Tweet tweet) =>
                {
                    var logger = Log.ForContext("Endpoint", "/tweets");
                    logger.Information("Received request to post a new tweet for UserId: {UserId}", tweet.UserId);

                    tweets.Add(tweet);

                    logger.Information("Tweet posted successfully. TweetId: {TweetId}", tweet.Id);
                    return Results.Ok(tweet);
                });

                // Get tweets by userId
                app.MapGet("/tweets", (int userId) =>
                {
                    var logger = Log.ForContext("Endpoint", "/tweets");
                    logger.Information("Received request to get tweets for UserId: {UserId}", userId);

                    var userTweets = tweets.Where(t => t.UserId == userId).ToList();

                    logger.Information("Returning {TweetCount} tweets for UserId: {UserId}", userTweets.Count, userId);
                    return Results.Ok(userTweets);
                });

                // Like a tweet (send RabbitMQ message to Notification Service)
                app.MapPost("/tweets/{id}/like", async (int id, LikeRequest request, IBus bus) =>
                {
                    var logger = Log.ForContext("Endpoint", "/tweets/{id}/like");
                    logger.Information("Received request to like TweetId: {TweetId} by UserId: {UserId}", id, request.UserId);

                    var tweet = tweets.FirstOrDefault(t => t.Id == id);

                    if (tweet is null)
                    {
                        logger.Warning("Tweet with TweetId {TweetId} not found", id);
                        return Results.NotFound();
                    }

                    logger.Information("Publishing like event for TweetId: {TweetId} by UserId: {UserId}", id, request.UserId);

                    // Send message to Notification Service using RabbitMQ
                    await bus.PubSub.PublishAsync(new TweetLikedMessage { TweetId = id, LikedByUserId = request.UserId });

                    logger.Information("Like event for TweetId: {TweetId} successfully published", id);
                    return Results.Ok();
                });

                // Map the health check endpoint
                app.MapHealthChecks("/health");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "PostManagementService terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
