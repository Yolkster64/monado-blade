using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Message for inter-service communication
    /// </summary>
    public class ServiceMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FromService { get; set; }
        public string ToService { get; set; }
        public string MessageType { get; set; }
        public object Payload { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Message handler delegate
    /// </summary>
    public delegate Task MessageHandler(ServiceMessage message);

    /// <summary>
    /// Inter-service communication bus
    /// </summary>
    public interface IInterServiceBus
    {
        void Subscribe(string serviceeName, string messageType, MessageHandler handler);
        void Unsubscribe(string serviceName, string messageType);
        Task PublishAsync(ServiceMessage message);
        Task<ServiceMessage> RequestAsync(ServiceMessage message, TimeSpan timeout);
    }

    /// <summary>
    /// In-process service message bus
    /// </summary>
    public class InterServiceBus : IInterServiceBus
    {
        private readonly Dictionary<string, Dictionary<string, List<MessageHandler>>> _subscriptions = new();
        private readonly object _subscriptionLock = new();
        private readonly Dictionary<string, TaskCompletionSource<ServiceMessage>> _pendingRequests = new();
        private readonly object _requestLock = new();
        private readonly EventBus _eventBus;

        public InterServiceBus(EventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void Subscribe(string serviceName, string messageType, MessageHandler handler)
        {
            lock (_subscriptionLock)
            {
                if (!_subscriptions.ContainsKey(serviceName))
                {
                    _subscriptions[serviceName] = new();
                }

                if (!_subscriptions[serviceName].ContainsKey(messageType))
                {
                    _subscriptions[serviceName][messageType] = new();
                }

                _subscriptions[serviceName][messageType].Add(handler);
            }
        }

        public void Unsubscribe(string serviceName, string messageType)
        {
            lock (_subscriptionLock)
            {
                if (_subscriptions.TryGetValue(serviceName, out var serviceMessages))
                {
                    serviceMessages.Remove(messageType);
                }
            }
        }

        public async Task PublishAsync(ServiceMessage message)
        {
            List<MessageHandler> handlers = null;

            lock (_subscriptionLock)
            {
                if (_subscriptions.TryGetValue(message.ToService, out var serviceMessages))
                {
                    if (serviceMessages.TryGetValue(message.MessageType, out var messageHandlers))
                    {
                        handlers = new List<MessageHandler>(messageHandlers);
                    }
                }
            }

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(message);
                    }
                    catch (Exception ex)
                    {
                        _eventBus?.PublishEvent(new Event
                        {
                            EventType = "MessageHandlerError",
                            Data = new { Error = ex.Message, MessageId = message.Id }
                        });
                    }
                }
            }
        }

        public async Task<ServiceMessage> RequestAsync(ServiceMessage message, TimeSpan timeout)
        {
            var requestId = message.Id;
            var tcs = new TaskCompletionSource<ServiceMessage>();

            lock (_requestLock)
            {
                _pendingRequests[requestId] = tcs;
            }

            try
            {
                await PublishAsync(message);
                return await tcs.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Message {requestId} timed out after {timeout.TotalSeconds}s");
            }
            finally
            {
                lock (_requestLock)
                {
                    _pendingRequests.Remove(requestId);
                }
            }
        }

        /// <summary>
        /// Sends a reply to a request message
        /// </summary>
        public void Reply(ServiceMessage request, object responsePayload)
        {
            lock (_requestLock)
            {
                if (_pendingRequests.TryGetValue(request.Id, out var tcs))
                {
                    var response = new ServiceMessage
                    {
                        Id = request.Id,
                        FromService = request.ToService,
                        ToService = request.FromService,
                        MessageType = request.MessageType + ".Response",
                        Payload = responsePayload,
                        Metadata = new() { { "IsReply", true } }
                    };
                    tcs.SetResult(response);
                }
            }
        }
    }
}
