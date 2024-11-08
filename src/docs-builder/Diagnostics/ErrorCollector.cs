using System.Diagnostics.CodeAnalysis;
using System.Text;
using Actions.Core;
using Actions.Core.Services;
using Cysharp.IO;
using Elastic.Markdown.Diagnostics;
using Errata;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Diagnostic = Elastic.Markdown.Diagnostics.Diagnostic;

namespace Documentation.Builder.Diagnostics;

public class FileSourceRepository : ISourceRepository
{
	public bool TryGet(string id, [NotNullWhen(true)] out Source? source)
	{
		using var reader = new Utf8StreamReader(id);
		var text = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
		source = new Source(id, text);
		return true;
	}
}

public class GithubAnnotationOutput(ICoreService githubActions) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTION"))) return;
		var properties = new AnnotationProperties
		{
			File = diagnostic.File,
			StartColumn = diagnostic.Column,
			StartLine = diagnostic.Line,
			EndColumn = diagnostic.Column + diagnostic.Length ?? 1
		};
		if (diagnostic.Severity == Severity.Error)
			githubActions.WriteError(diagnostic.Message, properties);
		if (diagnostic.Severity == Severity.Warning)
			githubActions.WriteWarning(diagnostic.Message, properties);
	}
}

public class ConsoleDiagnosticsCollector(ILoggerFactory loggerFactory, ICoreService? githubActions = null)
	: DiagnosticsCollector(loggerFactory, githubActions != null ? [new GithubAnnotationOutput(githubActions)] : [])
{
	private readonly List<Diagnostic> _items = new();

	private readonly ILogger<ConsoleDiagnosticsCollector> _logger =
		loggerFactory.CreateLogger<ConsoleDiagnosticsCollector>();

	protected override void HandleItem(Diagnostic diagnostic) => _items.Add(diagnostic);

	public override async Task StopAsync(Cancel ctx)
	{
		_logger.LogError("Stopping...");
		// Create a new report
		var report = new Report(new FileSourceRepository());
		foreach (var item in _items)
		{
			var d = item.Severity switch
			{
				Severity.Error =>
					Errata.Diagnostic.Error(item.Message)
						.WithLabel(new Label(item.File, new Location(item.Line, item.Column ?? 0), "bad substitution")
							.WithLength(item.Length ?? 3)
							.WithPriority(1)
							.WithColor(Color.Red)),
				Severity.Warning =>
					Errata.Diagnostic.Warning(item.Message),
				_ => Errata.Diagnostic.Info(item.Message)
			};
			report.AddDiagnostic(d);
		}

		// Render the report
		report.Render(AnsiConsole.Console);
		AnsiConsole.WriteLine();
		await Task.CompletedTask;
	}
}
