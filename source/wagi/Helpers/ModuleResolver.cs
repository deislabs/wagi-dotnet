namespace Deislabs.Wagi.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using Wasmtime;

    /// <summary>
    /// ModuleResolver resolves WASM Files into wasmtime Modules.
    /// </summary>
    public class ModuleResolver : IModuleResolver, IDisposable
    {
        private readonly Engine engine;

        private readonly Config config;

        private readonly ConcurrentDictionary<string, Module> modules;

        /// <summary>
        /// Gets the wasmtime Engine.
        /// </summary>
        public Engine Engine => engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleResolver"/> class.
        /// </summary>
        public ModuleResolver(Config config = null)
        {
            this.config = config ??= new Config();
            this.engine = new Engine(config);
            this.modules = new ConcurrentDictionary<string, Module>();
        }

        /// <summary>
        /// Gets the wasmtime Module from a filename.
        /// </summary>
        /// <param name="fileName">The wasm module filename.</param>
        /// <returns>The wasmtime Module.</returns>
        public Module GetWasmModule(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ApplicationException("FileName is Null");
            }

            return this.modules.GetOrAdd(fileName, (filename) =>
            {
                var moduleType = fileName.Split('.')[1].ToUpperInvariant();
                return moduleType switch
                {
                    "WASM" => Module.FromFile(engine, fileName),
                    "WAT" => Module.FromTextFile(engine, fileName),
                    _ => throw new ArgumentException($"invalid module type {moduleType} for File {fileName}"),
                };
            });
        }
        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var module in this.modules)
                {
                    module.Value.Dispose();
                }

                this.engine.Dispose();
                this.config.Dispose();
            }
        }

        /// <summary>
        /// IDisposable dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
