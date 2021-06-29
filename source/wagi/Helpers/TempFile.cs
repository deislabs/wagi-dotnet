namespace Deislabs.WAGI.Helpers
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides a wrapper around creating a temporary file.
    /// </summary>
    public class TempFile : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempFile"/> class.
        /// </summary>
        public TempFile()
          : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFile"/> class.
        /// </summary>
        /// <param name="createFile">Specifies if the file should be created.</param>
        public TempFile(bool createFile)
        {
            this.Path = System.IO.Path.GetTempFileName();
            if (createFile)
            {
                using var fs = File.Create(this.Path);
            }
        }

        /// <summary>
        /// Gets the temporary file path.
        /// </summary>
        public string Path { get; private set; }

#pragma warning disable CS1591
#pragma warning disable SA1600
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (File.Exists(this.Path))
                {
                    try
                    {
                        File.Delete(this.Path);
                    }
#pragma warning disable CA1031
                    catch
                    {
                        // Dont care if the file delete fails
                    }
                }
#pragma warning restore CA1031
            }
        }
    }
#pragma warning restore CS1591
#pragma warning restore SA1600
}
