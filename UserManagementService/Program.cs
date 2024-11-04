namespace UserManagementService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            // In-memory data store
            var users = new List<User> 
            {
                new User { Id = 1, Username = "john_doe", FollowedUsers = new List<int>() }
            };

            app.MapPost("/users", (User user) => 
            {
                users.Add(user);
                return Results.Ok(user);
            });

            app.MapGet("/users", () =>
            {
                return Results.Ok(users);

            });

            app.MapGet("/users/{id}", (int id) => 
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            app.MapPost("/users/{id}/follow/{followedId}", (int id, int followedId) => 
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                var followedUser = users.FirstOrDefault(u => u.Id == followedId);

                if (user is null || followedUser is null)
                    return Results.NotFound();

                user.FollowedUsers.Add(followedId);
                return Results.Ok();
            });

            app.Run();
        }

        public record User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public List<int> FollowedUsers { get; set; }
        }
    }
}
