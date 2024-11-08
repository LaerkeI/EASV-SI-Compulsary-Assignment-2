namespace TimelineService
{
    using System.Net.Http.Json;
    using Polly;

    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

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
                var httpClient = httpClientFactory.CreateClient("TimelineServiceClient");

                // Call User Management Service to get followed users
                var user = await httpClient.GetFromJsonAsync<User>($"http://user-management-service:80/users/{userId}");
                if (user == null) return Results.NotFound();

                var followedTweets = new List<Tweet>();

                // Fetch tweets from followed users
                foreach (var followedUserId in user.FollowedUsers)
                {
                    var tweets = await httpClient.GetFromJsonAsync<List<Tweet>>($"http://post-management-service:80/tweets?userId={followedUserId}");
                    if (tweets != null)
                        followedTweets.AddRange(tweets);
                }

                return Results.Ok(followedTweets);
            });

            // Map the health check endpoint
            app.MapHealthChecks("/health");

            app.Run();
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
