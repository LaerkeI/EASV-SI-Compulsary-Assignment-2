namespace NotificationService
{
    using EasyNetQ;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("/var/log/app/notification-service.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting NotificationService");

                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog to the logging pipeline
                builder.Host.UseSerilog();

                // Register health check services
                builder.Services.AddHealthChecks();

                // RabbitMQ setup
                var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
                Log.Information("Configuring RabbitMQ with host: {RabbitMqHost}", rabbitMqHost);
                var bus = RabbitHutch.CreateBus($"host={rabbitMqHost};timeout=60");

                // Add the bus to the DI container for later use
                builder.Services.AddSingleton<IBus>(bus);

                var app = builder.Build();

                // Subscribe to messages from PostManagement (tweets liked)
                Task.Run(() => SubscribeToMessages(bus));

                // Map the health check endpoint
                app.MapHealthChecks("/health");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "NotificationService terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void SubscribeToMessages(IBus bus)
        {
            try
            {
                Log.Information("NotificationService is now subscribing to messages...");

                // Subscribe to the TweetLikedMessage and handle it in a separate thread
                bus.PubSub.Subscribe<TweetLikedMessage>("notification-service", HandleTweetLikedMessage);

                Log.Information("Subscription to TweetLikedMessage is active.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while subscribing to messages.");
            }
        }

        private static void HandleTweetLikedMessage(TweetLikedMessage message)
        {
            var logger = Log.ForContext("MessageType", nameof(TweetLikedMessage));
            try
            {
                logger.Information("Processing TweetLikedMessage. TweetId: {TweetId}, LikedByUserId: {LikedByUserId}",
                    message.TweetId, message.LikedByUserId);

                // Simulate some message handling logic
                logger.Information("Tweet {TweetId} was liked by user {LikedByUserId}", message.TweetId, message.LikedByUserId);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred while processing TweetLikedMessage. TweetId: {TweetId}", message.TweetId);
            }
        }

        public class TweetLikedMessage
        {
            public int TweetId { get; set; }
            public int LikedByUserId { get; set; }
        }
    }
}
