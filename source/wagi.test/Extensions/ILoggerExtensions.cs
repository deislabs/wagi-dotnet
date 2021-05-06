namespace Deislabs.WAGI.Test.Extensions
{
  using System;
  using Moq;
  using Microsoft.Extensions.Logging;

  public static class MockExtensions
  {
    public static Mock<ILogger> VerifyLogError(this Mock<ILogger> logger, string expectedMessage)
    {
      Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;

      logger.Verify(
          x => x.Log(
              It.Is<LogLevel>(l => l == LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => state(v, t)),
              It.IsAny<Exception>(),
              It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

      return logger;
    }
  }
}
