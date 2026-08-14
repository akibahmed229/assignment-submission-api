using AssignmentSubmissionSystem.Api.Models.Enums;
using AssignmentSubmissionSystem.Api.Services;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Helpers;

public static class MockCurrentUser
{
    public static ICurrentUserService As(Guid userId, Role role)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.Role).Returns(role);
        return mock.Object;
    }
}
