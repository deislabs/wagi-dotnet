#pragma warning disable SA1402
#pragma warning disable SA1600
#pragma warning disable SA1649
#pragma warning disable CS1591
namespace Wasi.Experimental.Http.Exceptions
{
  using System;

  public abstract class ExperimentalHttpException : Exception
  {
    protected ExperimentalHttpException()
    {
    }

    protected ExperimentalHttpException(string message)
        : base(message)
    {
    }

    protected ExperimentalHttpException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public abstract int ErrorCode
    {
      get;
    }
  }

  public class InvalidHandleException : ExperimentalHttpException
  {
    public InvalidHandleException()
    {
    }

    public InvalidHandleException(string message)
        : base(message)
    {
    }

    public InvalidHandleException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 1; }
  }

  public class MemoryNotFoundException : ExperimentalHttpException
  {
    public MemoryNotFoundException()
    {
    }

    public MemoryNotFoundException(string message)
        : base(message)
    {
    }

    public MemoryNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 2; }
  }

  public class MemoryAccessException : ExperimentalHttpException
  {
    public MemoryAccessException()
    {
    }

    public MemoryAccessException(string message)
        : base(message)
    {
    }

    public MemoryAccessException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 3; }
  }

  public class BufferTooSmallException : ExperimentalHttpException
  {
    public BufferTooSmallException()
    {
    }

    public BufferTooSmallException(string message)
        : base(message)
    {
    }

    public BufferTooSmallException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 4; }
  }

  public class HeaderNotFoundException : ExperimentalHttpException
  {
    public HeaderNotFoundException()
    {
    }

    public HeaderNotFoundException(string message)
        : base(message)
    {
    }

    public HeaderNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 5; }
  }

  public class Utf8Exception : ExperimentalHttpException
  {
    public Utf8Exception()
    {
    }

    public Utf8Exception(string message)
        : base(message)
    {
    }

    public Utf8Exception(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 6; }
  }

  public class DestinationNotAllowedException : ExperimentalHttpException
  {
    public DestinationNotAllowedException()
    {
    }

    public DestinationNotAllowedException(string message)
        : base(message)
    {
    }

    public DestinationNotAllowedException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 7; }
  }

  public class InvalidMethodException : ExperimentalHttpException
  {
    public InvalidMethodException()
    {
    }

    public InvalidMethodException(string message)
        : base(message)
    {
    }

    public InvalidMethodException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 8; }
  }

  public class InvalidEncodingException : ExperimentalHttpException
  {
    public InvalidEncodingException()
    {
    }

    public InvalidEncodingException(string message)
        : base(message)
    {
    }

    public InvalidEncodingException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 9; }
  }

  public class InvalidUrlException : ExperimentalHttpException
  {
    public InvalidUrlException()
    {
    }

    public InvalidUrlException(string message)
        : base(message)
    {
    }

    public InvalidUrlException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 10; }
  }

  public class RequestException : ExperimentalHttpException
  {
    public RequestException()
    {
    }

    public RequestException(string message)
        : base(message)
    {
    }

    public RequestException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 11; }
  }

  public class RuntimeException : ExperimentalHttpException
  {
    public RuntimeException()
    {
    }

    public RuntimeException(string message)
        : base(message)
    {
    }

    public RuntimeException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 12; }
  }

  public class TooManySessionsException : ExperimentalHttpException
  {
    public TooManySessionsException()
    {
    }

    public TooManySessionsException(string message)
        : base(message)
    {
    }

    public TooManySessionsException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ErrorCode { get => 13; }
  }
}
#pragma warning restore SA1402
#pragma warning restore SA1600
#pragma warning restore SA1649
#pragma warning restore CS1591
