namespace Deislabs.WAGI.Test.Extensions
{
    using System;
    using Microsoft.Extensions.Logging;
    using Moq;

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

        private static Mock<ILogger> VerifyLog(Mock<ILogger> logger, string expectedMessage, LogLevel logLevel)
        {
            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;

            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == logLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => state(v, t)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

            return logger;
        }
    }
}
