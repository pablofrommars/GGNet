var builder = Host.CreateApplicationBuilder(args);

// stdio transport: stdout carries JSON-RPC — logging must go to stderr only.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly();

await builder.Build().RunAsync();
