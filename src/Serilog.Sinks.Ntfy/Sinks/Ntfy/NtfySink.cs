using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Serilog.Sinks.Ntfy
{
    public sealed class NtfySink : ILogEventSink, IDisposable
    {

        private const string AuthSchemeBasic = "Basic";
        private const string TitleHeader = "Title";
        private const string PriorityHeader = "Priority";
        private const string TagsHeader = "Tags";

        private readonly Channel<LogEvent> _channel;
        private readonly HttpClient _httpClient;
        private readonly Uri _endpoint;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _worker;
        private readonly bool _flushOnClose;
        private readonly int _maxRetryCount;
        private readonly TimeSpan _retryInterval;
        private readonly ITextFormatter _textFormatter;
        private readonly string[]? _tags;

        public NtfySink(
            string baseUrl,
            string topic,
            string[]? tags,
            string user,
            string password,
            ITextFormatter textFormatter,
            bool flushOnClose,
            int maxRetryCount,
            TimeSpan? retryInterval,
            int channelCapacity)
        {
            _endpoint = new Uri($"{baseUrl.TrimEnd('/')}/{topic}");
            _flushOnClose = flushOnClose;
            _textFormatter = textFormatter;
            _maxRetryCount = maxRetryCount;
            _retryInterval = retryInterval ?? TimeSpan.FromSeconds(2);
            _tags = tags;

            _httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(password))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        AuthSchemeBasic,
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
            }

            _channel = Channel.CreateBounded<LogEvent>(
                new BoundedChannelOptions(channelCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });

            _worker = Task.Run(ProcessQueueAsync);
        }

        public void Emit(LogEvent logEvent)
        {
            if (!_channel.Writer.TryWrite(logEvent))
            {
                SelfLog.WriteLine("NtfySink: Log event dropped due to full channel.");
            }
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                await foreach (var logEvent in _channel.Reader.ReadAllAsync(_cts.Token))
                {
                    await SendWithRetryAsync(logEvent);
                }
            }
            catch (OperationCanceledException)
            {
                // NOP
            }
            catch (Exception e)
            {
                SelfLog.WriteLine("Exception while emitting from {0}: {1}", this, e);
            }
        }

        private async Task SendWithRetryAsync(LogEvent logEvent)
        {
            for (var i = 0; i < _maxRetryCount; i++)
            {
                try
                {
                    using var content = CreateContent(logEvent);

                    var response = await _httpClient.PostAsync(
                        _endpoint,
                        content,
                        _cts.Token);

                    if (response.IsSuccessStatusCode) return;
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    // Cancellation requested, exit immediately
                    return;
                }
                catch when (i < _maxRetryCount)
                {
                    // Retry
                }

                await Task.Delay(_retryInterval, _cts.Token);
            }
        }

        private StringContent CreateContent(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            _textFormatter.Format(logEvent, writer);

            var content = new StringContent(writer.ToString(), Encoding.UTF8);

            content.Headers.Add(
                TitleHeader,
                $"[{logEvent.Level}] {logEvent.Timestamp:HH:mm:ss}");

            content.Headers.Add(
                PriorityHeader,
                logEvent.Level switch
                {
                    LogEventLevel.Fatal => NtfyPriority.Emergency.ToString("d"),
                    LogEventLevel.Error => NtfyPriority.High.ToString("d"),
                    LogEventLevel.Warning => NtfyPriority.Default.ToString("d"),
                    LogEventLevel.Information => NtfyPriority.Low.ToString("d"),
                    _ => NtfyPriority.Lowest.ToString("d"),
                });

            var tagHeader = _tags == null || _tags.Length == 0
                ? logEvent.Level.ToString()
                : $"{logEvent.Level},{string.Join(",", _tags)}";

            content.Headers.Add(TagsHeader, tagHeader);
            return content;
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();

            if (_flushOnClose)
            {
                _worker.GetAwaiter().GetResult();
            }
            else
            {
                _cts.Cancel();
            }

            _httpClient.Dispose();
            _cts.Dispose();
        }
    }

    internal enum NtfyPriority
    {
        Lowest = 1,
        Low = 2,
        Default = 3,
        High = 4,
        Emergency = 5
    }
}
