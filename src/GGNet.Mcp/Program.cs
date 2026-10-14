var builder = Host.CreateApplicationBuilder(args);

// INVARIANT: stdout carries JSON-RPC — all logging goes to stderr, or a stray line corrupts the protocol.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly();

await builder.Build().RunAsync();
