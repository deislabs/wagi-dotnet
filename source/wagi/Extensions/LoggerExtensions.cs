#pragma warning disable CS1591
namespace Deislabs.Wagi.Extensions

{
    using System;
    using Microsoft.Extensions.Logging;
    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, string, Exception> _volumeMappingFailed = LoggerMessage.Define<string, string>(
                LogLevel.Error,
                    new EventId(1, nameof(VolumeMappingFailed)),
                    "Error opening Volume. {Value} mapped to {Key} does not exist");

        private static readonly Action<ILogger, string, Exception> _addingWasiEnvVarFailed = LoggerMessage.Define<string>(
                LogLevel.Error,
                    new EventId(2, nameof(AddingWasiEnvVarFailed)),
                    "Failed to add environment variable {Key}");

        private static readonly Action<ILogger, int, Exception> _readBody = LoggerMessage.Define<int>(
                LogLevel.Error,
                    new EventId(3, nameof(ReadBody)),
                    "ReadBody called with handle {Handle}");

        private static readonly Action<ILogger, Exception> _traceException = LoggerMessage.Define(
                LogLevel.Trace,
                    new EventId(4, nameof(TraceException)),
                    "Exception Occurred");

        private static readonly Action<ILogger, string, string, double, int, long, Exception> _moduleExecutionTime = LoggerMessage.Define<string, string, double, int, long>(
                LogLevel.Trace,
                    new EventId(5, nameof(ModuleExecutionTime)),
                    "Call Module {WasmFile} Function {EntryPoint} Complete in {TotalSeconds:00}.{Milliseconds:000}{Ticks / 10 % 1000:000} seconds");

        private static readonly Action<ILogger, string, Exception> _createdDirectory = LoggerMessage.Define<string>(
                LogLevel.Trace,
                    new EventId(6, nameof(CreatedDirectory)),
                    "Creating Directory {Path}");
        private static readonly Action<ILogger, string, Exception> _traceMessage = LoggerMessage.Define<string>(
                LogLevel.Trace,
                    new EventId(7, nameof(TraceMessage)),
                    "{Message}");
        private static readonly Action<ILogger, string, Exception> _methodNotAllowed = LoggerMessage.Define<string>(
                LogLevel.Trace,
                    new EventId(8, nameof(MethodNotAllowed)),
                    "Request Method {Method} is not allowed");
        private static readonly Action<ILogger, string, string, string, Exception> _moduleWroteToStdErr = LoggerMessage.Define<string, string, string>(
                LogLevel.Warning,
                    new EventId(9, nameof(ModuleWroteToStdErr)),
                    "Stderr from Module {WasmFile} Function {EntryPoint}. Output:{Output}");
        private static readonly Action<ILogger, int, Exception> _invalidHandle = LoggerMessage.Define<int>(
                LogLevel.Trace,
                    new EventId(10, nameof(InvalidHandle)),
                    "Failed to get response Handle: {Handle}");
        private static readonly Action<ILogger, int, Exception> _closeCalled = LoggerMessage.Define<int>(
                LogLevel.Trace,
                    new EventId(11, nameof(CloseCalled)),
                    "Function close was called  with handle {Handle}");
        private static readonly Action<ILogger, string, string, Exception> _mappedWildcard = LoggerMessage.Define<string, string>(
                LogLevel.Trace,
                    new EventId(12, nameof(MappedWildcard)),
                    "Mapped Wildcard Route: {OriginalRoute} to {Route}");

        private static readonly Action<ILogger, string, string, Exception> _downloadingParcel = LoggerMessage.Define<string, string>(
                LogLevel.Trace,
                    new EventId(13, nameof(DownloadingParcel)),
                    "Downloading Parcel with InoviceId {InvoiceId} ParcelId {ParcelId}.");
        private static readonly Action<ILogger, string, Exception> _failedToAddModuleDefinedRoute = LoggerMessage.Define<string>(
                LogLevel.Error,
                    new EventId(14, nameof(FailedToAddModuleDefinedRoute)),
                    "Adding module defined route for Module Definition:{Name} Failed - skipping");
        private static readonly Action<ILogger, string, string, Exception> _addingModuleDefinedRoute = LoggerMessage.Define<string, string>(
                LogLevel.Trace,
                    new EventId(15, nameof(AddingModuleDefinedRoute)),
                    "Adding module defined route: {Route} EntryPoint: {EntryPoint}");
        private static readonly Action<ILogger, string, string, Exception> _addedRoute = LoggerMessage.Define<string, string>(
                LogLevel.Trace,
                    new EventId(16, nameof(AddedRoute)),
                    "Added Route: {Route} EntryPoint: {EntryPoint}");

        private static readonly Action<ILogger, string, Exception> _traceWarning = LoggerMessage.Define<string>(
                LogLevel.Warning,
                    new EventId(17, nameof(TraceWarning)),
                    "{Message}");
        private static readonly Action<ILogger, int, int, Exception> _bufferTooSmall = LoggerMessage.Define<int, int>(
                LogLevel.Warning,
                    new EventId(18, nameof(BufferTooSmall)),
                    "Buffer Too Small. Bufffer Length {BufferLength} Value Length {ValueLength}");
        public static void VolumeMappingFailed(this ILogger logger, string key, string value)
        {
            _volumeMappingFailed(logger, key, value, null);
        }

        public static void AddingWasiEnvVarFailed(this ILogger logger, string key, Exception ex)
        {
            _addingWasiEnvVarFailed(logger, key, ex);
        }

        public static void ReadBody(this ILogger logger, int handle)
        {
            _readBody(logger, handle, null);
        }

        public static void TraceException(this ILogger logger, Exception ex)
        {
            _traceException(logger, ex);
        }

        public static void ModuleExecutionTime(this ILogger logger, string wasmFile, string entryPoint, double totalSeconds, int milliseconds, long ticks)
        {
            _moduleExecutionTime(logger, wasmFile, entryPoint, totalSeconds, milliseconds, ticks, null);
        }

        public static void CreatedDirectory(this ILogger logger, string path)
        {
            _createdDirectory(logger, path, null);
        }

        public static void TraceMessage(this ILogger logger, string message, Exception ex = null)
        {
            _traceMessage(logger, message, ex);
        }

        public static void MethodNotAllowed(this ILogger logger, string method)
        {
            _methodNotAllowed(logger, method, null);
        }

        public static void ModuleWroteToStdErr(this ILogger logger, string module, string entryPoint, string output)
        {
            _moduleWroteToStdErr(logger, module, entryPoint, output, null);
        }

        public static void InvalidHandle(this ILogger logger, int handle)
        {
            _invalidHandle(logger, handle, null);
        }

        public static void CloseCalled(this ILogger logger, int handle)
        {
            _closeCalled(logger, handle, null);
        }

        public static void MappedWildcard(this ILogger logger, string originalRoute, string route)
        {
            _mappedWildcard(logger, originalRoute, route, null);
        }
        public static void DownloadingParcel(this ILogger logger, string invoiceId, string parcelId)
        {
            _downloadingParcel(logger, invoiceId, parcelId, null);
        }

        public static void FailedToAddModuleDefinedRoute(this ILogger logger, string name, Exception ex)
        {
            _failedToAddModuleDefinedRoute(logger, name, ex);
        }

        public static void AddingModuleDefinedRoute(this ILogger logger, string route, string entryPoint)
        {
            _addingModuleDefinedRoute(logger, route, entryPoint, null);
        }

        public static void AddedRoute(this ILogger logger, string route, string entryPoint)
        {
            _addedRoute(logger, route, entryPoint, null);
        }

        public static void TraceWarning(this ILogger logger, string message)
        {
            _traceWarning(logger, message, null);
        }

        public static void BufferTooSmall(this ILogger logger, int bufferLength, int valuesLength)
        {
            _bufferTooSmall(logger, bufferLength, valuesLength, null);
        }
    }
}
#pragma warning restore CS1591
