// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Extensions;
using ConsoleAppFramework;
using Documentation.Builder;
using Documentation.Builder.Cli;
using Elastic.Markdown.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddGitHubActionsCore();
services.AddLogging(x =>
{
	x.ClearProviders();
	x.SetMinimumLevel(LogLevel.Information);
	x.AddSimpleConsole(c =>
	{
		c.SingleLine = true;
		c.IncludeScopes = true;
		c.UseUtcTimestamp = true;
		c.TimestampFormat = Environment.UserInteractive ? ":: " : "[yyyy-MM-ddTHH:mm:ss] ";
	});
});
services.AddSingleton<DiagnosticsChannel>();
services.AddSingleton<DiagnosticsCollector>();

await using var serviceProvider = services.BuildServiceProvider();
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.Add<Commands>();

await app.RunAsync(args).ConfigureAwait(false);
