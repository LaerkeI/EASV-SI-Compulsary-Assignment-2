namespace NotificationService
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

            // Register health check services
            builder.Services.AddHealthChecks();

            // RabbitMQ setup
            var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
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

        private static void SubscribeToMessages(IBus bus)
        {
            // Subscribe to the TweetLikedMessage and handle it in a separate thread
            bus.PubSub.Subscribe<TweetLikedMessage>("notification-service", HandleTweetLikedMessage);

            Console.WriteLine("Notification Service is now listening for messages...");
        }

        private static void HandleTweetLikedMessage(TweetLikedMessage message)
        {
            Console.WriteLine($"Tweet {message.TweetId} was liked by user {message.LikedByUserId}");
        }

        public class TweetLikedMessage
        {
            public int TweetId { get; set; }
            public int LikedByUserId { get; set; }
        }
    }
}
