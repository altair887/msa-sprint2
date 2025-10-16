using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BookingService.Interceptors
{
    public class GrpcLoggingInterceptor : Interceptor
    {
        private readonly ILogger<GrpcLoggingInterceptor> _logger;

        public GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger)
        {
            _logger = logger;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Method;
            var peer = context.Peer;
            
            _logger.LogInformation("🔵 gRPC Connection Attempt: {Method} from {Peer} at {Timestamp}", 
                method, peer, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            try
            {
                _logger.LogDebug("📥 Incoming gRPC Request: {Method} - Request: {@Request}", 
                    method, request);

                var response = await continuation(request, context);
                
                stopwatch.Stop();
                
                _logger.LogInformation("✅ gRPC Success: {Method} from {Peer} - Duration: {Duration}ms - Status: {Status}", 
                    method, peer, stopwatch.ElapsedMilliseconds, context.Status.StatusCode);

                _logger.LogDebug("📤 Outgoing gRPC Response: {Method} - Response: {@Response}", 
                    method, response);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "❌ gRPC Error: {Method} from {Peer} - Duration: {Duration}ms - Error: {Error}", 
                    method, peer, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var peer = context.Peer;
            
            _logger.LogInformation("🔵 gRPC Client Streaming Connection: {Method} from {Peer} at {Timestamp}", 
                method, peer, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            try
            {
                var response = await continuation(requestStream, context);
                
                _logger.LogInformation("✅ gRPC Client Streaming Success: {Method} from {Peer} - Status: {Status}", 
                    method, peer, context.Status.StatusCode);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ gRPC Client Streaming Error: {Method} from {Peer} - Error: {Error}", 
                    method, peer, ex.Message);
                throw;
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var peer = context.Peer;
            
            _logger.LogInformation("🔵 gRPC Server Streaming Connection: {Method} from {Peer} at {Timestamp}", 
                method, peer, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            try
            {
                _logger.LogDebug("📥 Incoming gRPC Server Streaming Request: {Method} - Request: {@Request}", 
                    method, request);

                await continuation(request, responseStream, context);
                
                _logger.LogInformation("✅ gRPC Server Streaming Success: {Method} from {Peer} - Status: {Status}", 
                    method, peer, context.Status.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ gRPC Server Streaming Error: {Method} from {Peer} - Error: {Error}", 
                    method, peer, ex.Message);
                throw;
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var peer = context.Peer;
            
            _logger.LogInformation("🔵 gRPC Duplex Streaming Connection: {Method} from {Peer} at {Timestamp}", 
                method, peer, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            try
            {
                await continuation(requestStream, responseStream, context);
                
                _logger.LogInformation("✅ gRPC Duplex Streaming Success: {Method} from {Peer} - Status: {Status}", 
                    method, peer, context.Status.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ gRPC Duplex Streaming Error: {Method} from {Peer} - Error: {Error}", 
                    method, peer, ex.Message);
                throw;
            }
        }
    }
}
