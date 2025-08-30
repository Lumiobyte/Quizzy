using NUnit.Framework;
using Moq;
using Quizzy.Core.Services;
using Quizzy.Core.Repositories;
using Quizzy.Core.Entities;

namespace Quizzy.Testing
{
    [TestFixture]
    public sealed class LoginServiceTests
    {
        LoginService loginService;
        Mock<IUnitOfWork> mockRepository;
        Mock<IEmailService> mockEmailService;

        [SetUp]
        public void Setup()
        {
            mockRepository = new Mock<IUnitOfWork>();
            mockEmailService = new Mock<IEmailService>();
            loginService = new LoginService(mockRepository.Object, mockEmailService.Object);
        }

        [TestCase("correctUsername", "correctPassword", true)]
        [TestCase("incorrectUsername", "correctPassword", false)]
        [TestCase("correctUsername", "incorrectPassword", false)]
        [TestCase("incorrectUsername", "incorrectPassword", false)]
        public void TestLoginWithDetails(string username, string password, bool successful)
        {
            // Arrange
            mockRepository.Setup(r => r.UserAccounts.GetAllAsync()).ReturnsAsync(new[]
            {
                new Quizzy.Core.Entities.UserAccount { Username = "correctUsername", Password = "correctPassword", Id = Guid.NewGuid() }
            });

            // Act
            var user = loginService.LoginUser(username, password).Result;

            // Assert
            if (successful) Assert.That(user is not null);
            else Assert.That(user is null);
        }

        [TestCase("existingUser", "newPass", "new@example.com", "Username already exists")]
        [TestCase("newUser", "newPass", "existing@example.com", "Email already exists")]
        [TestCase("", "newPass", "new@example.com", "Username cannot be empty")]
        [TestCase("newUser", "", "new@example.com", "Password cannot be empty")]
        [TestCase("newUser", "newPass", "emal", "Email is not valid")]
        public void TestCreationFailsWithInvalidDetails(string username, string password, string email, string errorMsg)
        {
            // Arrange
            var existingUser = new UserAccount
            {
                Id = Guid.NewGuid(),
                Username = "existingUser",
                Password = "pass",
                Email = "existing@example.com"
            };
            mockRepository.Setup(r => r.UserAccounts.GetAllAsync())
                         .ReturnsAsync(new[] { existingUser });

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await loginService.CreateNewUser(username, password, email));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo(errorMsg));
            mockRepository.Verify(r => r.UserAccounts.AddAsync(It.IsAny<UserAccount>()), Times.Never);
            mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<UserAccount>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

        [Test]
        public void TestCreationSucceedsWithValidDetails()
        {
            // Arrange
            mockRepository.Setup(r => r.UserAccounts.GetAllAsync()).ReturnsAsync(Array.Empty<UserAccount>());
            UserAccount? addedUser = null;
            mockRepository.Setup(r => r.UserAccounts.AddAsync(It.IsAny<UserAccount>()))
                          .Callback<UserAccount>(u => addedUser = u)
                          .Returns(Task.CompletedTask);
            mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<UserAccount>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(Task.CompletedTask);

            // Act
            var userId = loginService.CreateNewUser("newUser", "newPass", "new@email.com").GetAwaiter().GetResult();

            // Assert
            Assert.That(addedUser, Is.Not.Null);
            Assert.That(addedUser!.Id, Is.EqualTo(userId));
            Assert.That(addedUser.Username, Is.EqualTo("newUser"));
            Assert.That(addedUser.Password, Is.EqualTo("newPass"));
            Assert.That(addedUser.Email, Is.EqualTo("new@email.com"));
            mockRepository.Verify(r => r.UserAccounts.AddAsync(It.IsAny<UserAccount>()), Times.Once); 
            mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<UserAccount>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }
    }
}
