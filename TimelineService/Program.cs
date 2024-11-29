namespace TimelineService
{
    using Serilog;
    using Polly;
    using System.Net.Http.Json;
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("/var/log/app/timeline-service.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting TimelineService");

                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog to the logging pipeline
                builder.Host.UseSerilog();

                // Register health check services
                builder.Services.AddHealthChecks();

                // Configure HttpClient with Polly policies
                builder.Services.AddHttpClient("TimelineServiceClient")
                    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500)))
                    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

                var app = builder.Build();

                // Timeline (get tweets from followed users)
                app.MapGet("/timeline/{userId}", async (int userId, IHttpClientFactory httpClientFactory) =>
                {
                    var logger = Log.ForContext("Endpoint", "/timeline/{userId}");
                    logger.Information("Received request to fetch timeline for UserId: {UserId}", userId);

                    var httpClient = httpClientFactory.CreateClient("TimelineServiceClient");

                    // Call User Management Service to get followed users
                    var user = await httpClient.GetFromJsonAsync<User>($"http://user-management-service:80/users/{userId}");
                    if (user == null)
                    {
                        logger.Warning("User with UserId {UserId} not found", userId);
                        return Results.NotFound();
                    }

                    logger.Information("User found: {Username}, fetching tweets for followed users", user.Username);

                    var followedTweets = new List<Tweet>();

                    // Fetch tweets from followed users
                    foreach (var followedUserId in user.FollowedUsers)
                    {
                        var tweets = await httpClient.GetFromJsonAsync<List<Tweet>>($"http://post-management-service:80/tweets?userId={followedUserId}");
                        if (tweets != null)
                        {
                            followedTweets.AddRange(tweets);
                            logger.Information("Fetched {TweetCount} tweets for FollowedUserId: {FollowedUserId}", tweets.Count, followedUserId);
                        }
                        else
                        {
                            logger.Warning("No tweets found for FollowedUserId: {FollowedUserId}", followedUserId);
                        }
                    }

                    logger.Information("Successfully fetched timeline for UserId: {UserId}", userId);
                    return Results.Ok(followedTweets);
                });

                // Map the health check endpoint
                app.MapHealthChecks("/health");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "TimelineService terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public record User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public List<int> FollowedUsers { get; set; }
        }

        public record Tweet
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string Content { get; set; }
        }
    }
}
