#pragma warning disable SA1600
namespace Wasi.Experimental.Http
{
  using System;
  using System.IO;
  using System.Net.Http;

  internal class Response : IDisposable
  {
    private Stream content;

    public Response(HttpResponseMessage httpResponseMessage)
    {
      this.HttpResponseMessage = httpResponseMessage;
    }

    public HttpResponseMessage HttpResponseMessage { get; }

    public Stream Content
    {
      get
      {
        if (this.content == null)
        {
          this.content = this.HttpResponseMessage.Content.ReadAsStream();
        }

        return this.content;
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        this.HttpResponseMessage.Dispose();
        if (this.content != null)
        {
          this.content.Dispose();
        }
      }
    }
  }
#pragma warning restore SA1600
}
