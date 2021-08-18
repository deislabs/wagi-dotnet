namespace Deislabs.Wagi.Test.Extensions
{
    using System;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public static class MockExtensions
    {
        public static Mock<ILogger> VerifyLogError(this Mock<ILogger> logger, string expectedMessage)
        {
            MockExtensions.VerifyLog(logger, expectedMessage, LogLevel.Error);
            return logger;
        }

        public static Mock<ILogger> VerifyLogTrace(this Mock<ILogger> logger, string expectedMessage)
        {
            MockExtensions.VerifyLog(logger, expectedMessage, LogLevel.Trace);
            return logger;
        }

        public static Mock<ILogger> VerifyLogWarning(this Mock<ILogger> logger, string expectedMessage)
        {
            MockExtensions.VerifyLog(logger, expectedMessage, LogLevel.Warning);
            return logger;
        }

        private static Mock<ILogger> VerifyLog(Mock<ILogger> logger, string expectedMessage, LogLevel expectedLogLevel, Times? times = null)
        {
            times ??= Times.Once();

            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(logLevel => logLevel == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((message, type) => CheckExpectedMessage(message, expectedMessage)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), times.Value);

            return logger;
        }

        private static bool CheckExpectedMessage(object message, string expectedMessage)
            => message.ToString().CompareTo(expectedMessage) == 0;
    }
}
