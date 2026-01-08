# Serilog.Sinks.Ntfy

`Serilog.Sinks.Ntfy` is a Serilog sink that publishes log events as notifications to an ntfy-compatible server (for example, `https://ntfy.sh`). It is designed for .NET 8 and modern C# projects, providing a simple, reliable way to surface critical logs as push notifications.

## Project Description

`Serilog.Sinks.Ntfy` publishes log events as notifications to an ntfy-compatible server.

### Key features

- Publish log events to an ntfy topic using HTTP.
- Support for Basic (username/password) and Bearer (access token) authentication.
- Configurable message formatting via message templates or custom `ITextFormatter` implementations (e.g., JSON output).
- Retry logic with configurable maximum retry attempts and retry interval.
- In-memory buffering with configurable `channelCapacity` and optional flush-on-close behavior to ensure buffered events are delivered when the sink is disposed.
- Supports runtime level control via `LoggingLevelSwitch` and standard Serilog minimum-level filtering.

### Quick start

Install the package from NuGet and configure Serilog in your application: https://www.nuget.org/packages/Serilog.Sinks.Ntfy

### Configuration notes

- Provide both `user` and `password` together for Basic auth; providing only one will result in an error.
- When using `accessToken`, pass an empty string for the user parameter (internally the sink distinguishes auth mode).
- Tune `maxRetryCount`, `retryInterval`, `channelCapacity`, and `flushOnClose` to match reliability and throughput requirements.

### Note:
Although netstandard2.0 is supported, this package relies on
System.Threading.Channels which is provided via NuGet.

## License

This project is open source — see the repository license for details.
