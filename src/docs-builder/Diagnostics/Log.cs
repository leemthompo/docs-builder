// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Documentation.Builder;

// named Log for terseness on console output
public class Log(ILogger logger) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			logger.LogError($"{diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
		else
			logger.LogWarning($"{diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
	}
}

