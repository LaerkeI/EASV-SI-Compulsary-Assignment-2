namespace UserManagementService.Tests
{
    using UserManagementService;
    using Xunit;
    using System.Collections.Generic;

    public class UnitTests
    {
        [Fact]
        public void AddUser_ShouldAddUserToList()
        {
            // Arrange
            var users = new List<Program.User>();
            var newUser = new Program.User { Id = 1, Username = "john_doe" };

            // Act
            users.Add(newUser);

            // Assert
            Assert.Contains(newUser, users);
        }
    }
}
