namespace ApiGateway.Tests
{
    using ApiGateway;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using Xunit;

    public class UnitTests
    {
        [Fact]
        public void GenerateJwtToken_ShouldReturnValidToken()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(config => config["JwtSettings:SecretKey"]).Returns("supersecretkey123");

            int userId = 1;
            string username = "testuser";

            // Act
            var token = Program.GenerateJwtToken(userId, username, mockConfig.Object);

            // Assert
            Assert.NotNull(token);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal(userId.ToString(), jwtToken.Subject);
            Assert.Equal(username, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowException_WhenSecretKeyIsMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(config => config["JwtSettings:SecretKey"]).Returns(string.Empty); // Simulating missing secret key

            int userId = 1;
            string username = "testuser";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => Program.GenerateJwtToken(userId, username, mockConfig.Object));
            Assert.Contains("IDX10703", exception.Message);  // Verifies the specific exception for empty key
        }
    }
}
