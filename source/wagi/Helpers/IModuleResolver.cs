namespace Deislabs.Wagi.Helpers
{
    using Wasmtime;

    /// <summary>
    /// IModuleResolver contains methods for resolving WASM Modules.
    /// </summary>
    public interface IModuleResolver
    {
        /// <summary>
        /// Gets the wasmtime Engine.
        /// </summary>

        public Engine Engine { get; }

        /// <summary>
        /// Gets the WASM Module from a filename.
        /// </summary>
        /// <param name="fileName">The WASM File name.</param>
        /// <returns>A WASM Module.</returns>
        public Module GetWasmModule(string fileName);
    }
}
