using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;

namespace Serilog.Sinks.Ntfy
{
    public static class NtfyLoggerConfigurationExtensions
    {
        private const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Notify log events via ntfy.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="baseUrl">ntfy server URL (e.g.: <c>https://ntfy.sh</c>).</param>
        /// <param name="topic">ntfy topic for publishing messages.</param>
        /// <param name="tags">Tags shown below the notification.</param>
        /// <param name="user">Username for Basic authentication when notifying the ntfy server. Default is <see cref="string.Empty"/>.</param>
        /// <param name="password">Password for Basic authentication when notifying the ntfy server. Default is <see cref="string.Empty"/>.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// The default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information. Default is <see langword="null"/>.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. 
        /// Ignored when <paramref name="levelSwitch"/> is specified. Default is <see cref="LogEventLevel.Verbose"/>.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime. Default is <see langword="null"/>.</param>
        /// <param name="maxRetryCount">Maximum number of retry attempts when sending fails. Default is 3.</param>
        /// <param name="retryInterval">Interval between retry attempts. Default is <see langword="null"/>.</param>
        /// <param name="flushOnClose">Whether to send log events buffered on disk when the sink is disposed, 
        /// thus ensuring that all generated log events are sent to the ntfy server before the sink closes. Default value is <see langword="true"/>.</param>
        /// <param name="channelCapacity">Maximum capacity for buffering log events. Default is 1000.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sinkConfiguration"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">When <paramref name="baseUrl"/> or <paramref name="topic"/> is <see langword="null"/> or <see cref="string.Empty"/>.</exception>
        public static LoggerConfiguration Ntfy(
            this LoggerSinkConfiguration sinkConfiguration,
            string baseUrl,
            string topic,
            string[]? tags = null,
            string user = "",
            string password = "",
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider? formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = null,
            int maxRetryCount = 3,
            TimeSpan? retryInterval = null,
            bool flushOnClose = true,
            int channelCapacity = 1000)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException("Topic cannot be null or empty.", nameof(topic));
            }

            ValidateCredentials(user, password);

            var textFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return sinkConfiguration.Sink(new NtfySink(baseUrl, topic, tags, user, password, textFormatter, flushOnClose, maxRetryCount, retryInterval, channelCapacity),
                restrictedToMinimumLevel,
                levelSwitch);
        }

        /// <summary>
        /// Notify log events via ntfy.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="baseUrl">ntfy server URL (e.g.: <c>https://ntfy.sh</c>).</param>
        /// <param name="topic">ntfy topic for publishing messages.</param>
        /// <param name="formatter">A formatter (e.g. <see cref="JsonFormatter"/>) to convert log events into text for the notification.
        /// If control of text formatting via a template is required, use the overload that accepts an output template.</param>
        /// <param name="tags">Tags shown below the notification.</param>
        /// <param name="user">Username for Basic authentication when notifying the ntfy server. Default is <see cref="string.Empty"/>.</param>
        /// <param name="password">Password for Basic authentication when notifying the ntfy server. Default is <see cref="string.Empty"/>.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. 
        /// Ignored when <paramref name="levelSwitch"/> is specified. Default is <see cref="LogEventLevel.Verbose"/>.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime. Default is <see langword="null"/>.</param>
        /// <param name="maxRetryCount">Maximum number of retry attempts when sending fails. Default is 3.</param>
        /// <param name="retryInterval">Interval between retry attempts. Default is <see langword="null"/>.</param>
        /// <param name="flushOnClose">Whether to send log events buffered on disk when the sink is disposed, 
        /// thus ensuring that all generated log events are sent to the ntfy server before the sink closes. Default value is <see langword="true"/>.</param>
        /// <param name="channelCapacity">Maximum capacity for buffering log events. Default is 1000.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sinkConfiguration"/> or <paramref name="formatter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">When <paramref name="baseUrl"/> or <paramref name="topic"/> is <see langword="null"/> or <see cref="string.Empty"/>.</exception>
        public static LoggerConfiguration Ntfy(
            this LoggerSinkConfiguration sinkConfiguration,
            string baseUrl,
            string topic,
            ITextFormatter formatter,
            string[]? tags = null,
            string user = "",
            string password = "",
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = null,
            int maxRetryCount = 3,
            TimeSpan? retryInterval = null,
            bool flushOnClose = true,
            int channelCapacity = 1000)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException("Topic cannot be null or empty.", nameof(topic));
            }

            ValidateCredentials(user, password);

            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            return sinkConfiguration.Sink(new NtfySink(baseUrl, topic, tags, user, password, formatter, flushOnClose, maxRetryCount, retryInterval, channelCapacity),
                restrictedToMinimumLevel,
                levelSwitch);
        }

        /// <summary>
        /// Notify log events via ntfy.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="baseUrl">ntfy server URL (e.g.: <c>https://ntfy.sh</c>).</param>
        /// <param name="topic">ntfy topic for publishing messages.</param>
        /// <param name="accessToken">Access token for Bearer authentication when notifying the ntfy server.</param>
        /// <param name="tags">Tags shown below the notification.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// The default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information. Default is <see langword="null"/>.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. 
        /// Ignored when <paramref name="levelSwitch"/> is specified. Default is <see cref="LogEventLevel.Verbose"/>.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime. Default is <see langword="null"/>.</param>
        /// <param name="maxRetryCount">Maximum number of retry attempts when sending fails. Default is 3.</param>
        /// <param name="retryInterval">Interval between retry attempts. Default is <see langword="null"/>.</param>
        /// <param name="flushOnClose">Whether to send log events buffered on disk when the sink is disposed, 
        /// thus ensuring that all generated log events are sent to the ntfy server before the sink closes. Default value is <see langword="true"/>.</param>
        /// <param name="channelCapacity">Maximum capacity for buffering log events. Default is 1000.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sinkConfiguration"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">When <paramref name="baseUrl"/> or <paramref name="topic"/> or <paramref name="accessToken"/> is <see langword="null"/> or <see cref="string.Empty"/>.</exception>
        public static LoggerConfiguration Ntfy(
            this LoggerSinkConfiguration sinkConfiguration,
            string baseUrl,
            string topic,
            string accessToken,
            string[]? tags = null,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider? formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = null,
            int maxRetryCount = 3,
            TimeSpan? retryInterval = null,
            bool flushOnClose = true,
            int channelCapacity = 1000)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException("Topic cannot be null or empty.", nameof(topic));
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            }

            var textFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return sinkConfiguration.Sink(new NtfySink(baseUrl, topic, tags, string.Empty, accessToken, textFormatter, flushOnClose, maxRetryCount, retryInterval, channelCapacity),
                restrictedToMinimumLevel,
                levelSwitch);
        }

        /// <summary>
        /// Notify log events via ntfy.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="baseUrl">ntfy server URL (e.g.: <c>https://ntfy.sh</c>).</param>
        /// <param name="topic">ntfy topic for publishing messages.</param>
        /// <param name="accessToken">Access token for Bearer authentication when notifying the ntfy server.</param>
        /// <param name="formatter">A formatter (e.g. <see cref="JsonFormatter"/>) to convert log events into text for the notification.
        /// If control of text formatting via a template is required, use the overload that accepts an output template.</param>
        /// <param name="tags">Tags shown below the notification.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. 
        /// Ignored when <paramref name="levelSwitch"/> is specified. Default is <see cref="LogEventLevel.Verbose"/>.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime. Default is <see langword="null"/>.</param>
        /// <param name="maxRetryCount">Maximum number of retry attempts when sending fails. Default is 3.</param>
        /// <param name="retryInterval">Interval between retry attempts. Default is <see langword="null"/>.</param>
        /// <param name="flushOnClose">Whether to send log events buffered on disk when the sink is disposed, 
        /// thus ensuring that all generated log events are sent to the ntfy server before sink closes. Default value is <see langword="true"/>.</param>
        /// <param name="channelCapacity">Maximum capacity for buffering log events. Default is 1000.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sinkConfiguration"/> or <paramref name="formatter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">When <paramref name="baseUrl"/> or <paramref name="topic"/> or <paramref name="accessToken"/> is <see langword="null"/> or <see cref="string.Empty"/>.</exception>
        public static LoggerConfiguration Ntfy(
            this LoggerSinkConfiguration sinkConfiguration,
            string baseUrl,
            string topic,
            string accessToken,
            ITextFormatter formatter,
            string[]? tags = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = null,
            int maxRetryCount = 3,
            TimeSpan? retryInterval = null,
            bool flushOnClose = true,
            int channelCapacity = 1000)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException("Topic cannot be null or empty.", nameof(topic));
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            }

            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            return sinkConfiguration.Sink(new NtfySink(baseUrl, topic, tags, string.Empty, accessToken, formatter, flushOnClose, maxRetryCount, retryInterval, channelCapacity),
                restrictedToMinimumLevel,
                levelSwitch);
        }

        /// <summary>
        /// Validate the input status for Basic Authentication when sending notifications to an ntfy server.
        /// Both username and password must be specified, or neither should be specified.
        /// </summary>
        /// <param name="user">Username for Basic authentication</param>
        /// <param name="password">Password for Basic authentication</param>
        /// <exception cref="ArgumentException">When exactly one of <paramref name="user"/> or <paramref name="password"/> is <see langword="null"/> or <see cref="string.Empty"/>.</exception>
        private static void ValidateCredentials(string user, string password)
        {
            var hasUser = !string.IsNullOrEmpty(user);
            var hasPassword = !string.IsNullOrEmpty(password);

            if (hasUser ^ hasPassword)
            {
                throw new ArgumentException("Both user and password must be provided together.");
            }
        }
    }
}
