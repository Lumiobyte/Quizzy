using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Quizzy.Core.DTOs;
using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using Quizzy.Web.Hubs;
using Quizzy.Web.Services;
using System.Reflection;

namespace Quizzy.Testing
{
    [TestFixture]
    public class GameHubTests
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private SessionCoordinator _sessions = null!;
        private Mock<IServiceScopeFactory> _scopeFactory = null!;
        private GameHub _hub = null!;
        private Mock<IQuizPlayerRepository> _playerRepo;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _sessions = new SessionCoordinator();
            _scopeFactory = new Mock<IServiceScopeFactory>();
            _hub = new GameHub(_sessions, _unitOfWork.Object, _scopeFactory.Object);
            _playerRepo = new Mock<IQuizPlayerRepository>();

            var mockContext = new Mock<HubCallerContext>();
            mockContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            _hub.Context = mockContext.Object;

            var mockClients = new Mock<IHubCallerClients>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());
            _hub.Clients = mockClients.Object;
            _hub.Groups = new Mock<IGroupManager>().Object;
        }

        private static T InvokePrivate<T>(string name, params object?[] args)
        {
            var method = typeof(GameHub).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Method {name} not found");
            var result = method!.Invoke(method.IsStatic ? null : new GameHub(new SessionCoordinator(), new Mock<IUnitOfWork>().Object, new Mock<IServiceScopeFactory>().Object), args);
            return (T)result!;
        }

        [Test]
        public void GeneratePin_ReturnsCorrectLength()
        {
            var method = typeof(GameHub).GetMethod("GeneratePin", BindingFlags.NonPublic | BindingFlags.Static);
            var pin = (string)method!.Invoke(null, new object[] { 6 })!;
            Assert.That(pin.Length, Is.EqualTo(6));
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Assert.That(pin.All(c => chars.Contains(c)), Is.True);
        }

        [Test]
        public void BuildQuestionStartedDto_FormatsOptions()
        {
            var question = new QuizQuestion
            {
                Text = "Q",
                QuestionType = Quizzy.Core.Enums.QuestionType.MultipleChoice,
                Answers = new List<QuizAnswer>
                {
                    new QuizAnswer{ Text = " A " },
                    new QuizAnswer{ Text = "B" }
                }
            };
            var method = typeof(GameHub).GetMethod("BuildQuestionStartedDto", BindingFlags.NonPublic | BindingFlags.Static);
            var dto = (QuestionStartedDto)method!.Invoke(null, new object[] { question })!;
            Assert.That(dto.Question, Is.EqualTo("Q"));
            CollectionAssert.AreEqual(new[] { "A", "B" }, dto.Options);
            Assert.That(dto.DurationSeconds, Is.EqualTo(10));
        }

        [Test]
        public void GetQuizQuestionAtIndex_ReturnsQuestion()
        {
            var q1 = new QuizQuestion { Text = "1" };
            var q2 = new QuizQuestion { Text = "2" };
            var method = typeof(GameHub).GetMethod("GetQuizQuestionAtIndex", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (QuizQuestion)method!.Invoke(null, new object[] { 1, new List<QuizQuestion> { q1, q2 } })!;
            Assert.That(result, Is.EqualTo(q2));
        }

        [Test]
        public void GetQuizQuestionAtIndex_ThrowsOnInvalid()
        {
            var q1 = new QuizQuestion { Text = "1" };
            var method = typeof(GameHub).GetMethod("GetQuizQuestionAtIndex", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, new object[] { 5, new List<QuizQuestion> { q1 } }));
        }

        [Test]
        public void GetQuizQuestionsAsync_ThrowsWhenPinEmpty()
        {
            var method = typeof(GameHub).GetMethod("GetQuizQuestionsAsync", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await (Task<List<QuizQuestion>>)method!.Invoke(null, new object[] { _unitOfWork.Object, "" })!;
            });
        }

        [Test]
        public void BuildStateDto_UsesUppercasePin()
        {
            var players = new List<QuizPlayer>();
            var runtime = new SessionRuntime(new QuizSession());
            var method = typeof(GameHub).GetMethod("BuildStateDto", BindingFlags.NonPublic | BindingFlags.Static);
            var dto = (SessionStateDto)method!.Invoke(null, new object[] { "abc", runtime, players })!;
            Assert.That(dto.SessionId, Is.EqualTo("ABC"));
        }

        [Test]
        public void EnsureSessionForPin_ThrowsWhenMissing()
        {
            _unitOfWork.Setup(u => u.QuizSessions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuizSession, bool>>>()
            )).ReturnsAsync(Array.Empty<QuizSession>());
            var method = typeof(GameHub).GetMethod("EnsureSessionForPin", BindingFlags.NonPublic | BindingFlags.Instance);
            var ex = Assert.Throws<TargetInvocationException>(() => method!.Invoke(_hub, new object[] { "abc" }));
            Assert.That(ex!.InnerException, Is.InstanceOf<HubException>());
        }

        [Test]
        public void ResolveHostAccountAsync_ReturnsUserById()
        {
            var id = Guid.NewGuid();
            _unitOfWork.Setup(u => u.UserAccounts.GetByIdAsync(id)).ReturnsAsync(new UserAccount { Id = id });
            var method = typeof(GameHub).GetMethod("ResolveHostAccountAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var user = (Task<UserAccount>)method!.Invoke(_hub, new object?[] { id.ToString() })!;
            Assert.That(user.Result.Id, Is.EqualTo(id));
        }

        [Test]
        public void EnsureSessionForPinWithHostAndQuiz_CreatesSession()
        {
            var host = new UserAccount { Id = Guid.NewGuid() };
            var quiz = new Quiz { Id = Guid.NewGuid() };
            _unitOfWork.Setup(u => u.QuizSessions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuizSession, bool>>>()
            )).ReturnsAsync(Array.Empty<QuizSession>());
            _unitOfWork.Setup(u => u.Quizzes.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(quiz);
            var method = typeof(GameHub).GetMethod("EnsureSessionForPinWithHostAndQuiz", BindingFlags.NonPublic | BindingFlags.Instance);
            var session = (QuizSession)method!.Invoke(_hub, new object[] { "PIN", host, quiz.Id })!;
            Assert.That(session.GamePin, Is.EqualTo("PIN"));
        }

        [Test]
        public void ClaimHost_ThrowsWhenPinMissing()
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await _hub.ClaimHost(""));
        }

        [Test]
        public void CreateAndClaimSession_ThrowsWhenSessionMissing()
        {
            _unitOfWork.Setup(u => u.QuizSessions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuizSession, bool>>>()
            )).ReturnsAsync(Array.Empty<QuizSession>());
            Assert.ThrowsAsync<HubException>(async () => await _hub.CreateAndClaimSession());
        }

        [Test]
        public void CreateAndClaimSessionForQuiz_ThrowsWhenHostMissing()
        {
            _unitOfWork.Setup(u => u.UserAccounts.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserAccount?)null);
            Assert.ThrowsAsync<HubException>(async () => await _hub.CreateAndClaimSessionForQuiz(Guid.NewGuid().ToString(), Guid.NewGuid()));
        }

        [Test]
        public void ScheduleNextQuestion_ThrowsOnEmptyPin()
        {
            Assert.ThrowsAsync<HubException>(async () => await _hub.ScheduleNextQuestion("", 0, 0));
        }

        [Test]
        public void JoinAsPlayer_ThrowsOnEmptyPin()
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await _hub.JoinAsPlayer("", "name", Guid.NewGuid().ToString()));
        }

        [Test]
        public void JoinAsPlayerWithoutLogin_NotImplemented()
        {
            Assert.ThrowsAsync<NotImplementedException>(async () => await _hub.JoinAsPlayerWithoutLogin("PIN", "name"));
        }

        [Test]
        public void SubmitAnswer_ThrowsOnEmptyPin()
        {
            Assert.ThrowsAsync<HubException>(async () => await _hub.SubmitAnswer("", 0));
        }

        [Test]
        public void EndCurrentQuestion_ThrowsOnEmptyPin()
        {
            Assert.ThrowsAsync<HubException>(async () => await _hub.EndCurrentQuestion(""));
        }

        [Test]
        public void BroadcastSessionState_SendsUpdate()
        {
            var runtime = new SessionRuntime(new QuizSession());
            _unitOfWork.Setup(u => u.QuizPlayers.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuizPlayer, bool>>>()
            )).ReturnsAsync(Array.Empty<QuizPlayer>());
            var mockClientProxy = new Mock<IClientProxy>();
            var mockClients = new Mock<IHubCallerClients>();
            mockClients.Setup(c => c.Group("PIN"))
                .Returns(mockClientProxy.Object);
            _hub.Clients = mockClients.Object;
            var method = typeof(GameHub).GetMethod("BroadcastSessionState", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(_hub, new object[] { "PIN", runtime });
            mockClientProxy.Verify(c => c.SendCoreAsync("SessionStateUpdated", It.IsAny<object?[]>(), default), Times.Once);
        }
    }
}