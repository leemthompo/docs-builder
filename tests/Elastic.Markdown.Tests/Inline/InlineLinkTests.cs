// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LinkTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(output, content)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
"""
# Special Requirements

To follow this tutorial you will need to install the following components:
""";
		fileSystem.AddFile(@"docs/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/_static/img/observability.png", new MockFileData(""));
	}

}

public class InlineLinkTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[Elasticsearch](/_static/img/observability.png)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="/_static/img/observability.png">Elasticsearch</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class LinkToPageTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[Requirements](testing/req.md)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html">Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(0);
	}
}

public class InsertPageTitleTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[](testing/req.md)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html">Special Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(0);
	}
}

public class LinkReferenceTest(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[test][test]

	[test]: testing/req.md
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(0);
	}
}

public class CrossLinkReferenceTest(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[test][test]

	[test]: kibana://index.md
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			// TODO: The link is not rendered correctly yet, will be fixed in a follow-up
			"""<p><a href="kibana://index.md">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class CrossLinkTest(ITestOutputHelper output) : LinkTestBase(output,
	"""

	Go to [test](kibana://index.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			// TODO: The link is not rendered correctly yet, will be fixed in a follow-up
			"""<p>Go to <a href="kibana://index.md">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class LinksWithInterpolationWarning(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[global search field]({{kibana-ref}}/introduction.html#kibana-navigation-search)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="%7B%7Bkibana-ref%7D%7D/introduction.html#kibana-navigation-search">global search field</a></p>"""
		);

	[Fact]
	public void HasWarnings()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.First().Severity.Should().Be(Diagnostics.Severity.Warning);
		Collector.Diagnostics.First().Message.Should().Contain("The url contains a template expression. Please do not use template expressions in links. See https://github.com/elastic/docs-builder/issues/182 for further information.");
	}
}

public class CommentedNonExistingLinks(ITestOutputHelper output) : LinkTestBase(output,
	"""
	% [Non Existing Link](/non-existing.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().BeNullOrWhiteSpace();

	[Fact]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CommentedNonExistingLinks2(ITestOutputHelper output) : LinkTestBase(output,
	"""
	% Hello, this is a [Non Existing Link](/non-existing.md).
	Links:
	- [](/testing/req.md)
	% - [Non Existing Link](/non-existing.md)
	- [](/testing/req.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.TrimEnd().Should().Be("""
		<p>Links:</p>
		<ul>
		<li> <a href="/testing/req.html">Special Requirements</a></li>
		</ul>
		<ul>
		<li> <a href="/testing/req.html">Special Requirements</a></li>
		</ul>
		""");

	[Fact]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class NonExistingLinkShouldFail(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[Non Existing Link](/non-existing.md)
	- [Non Existing Link](/non-existing.md)
	This is another [Non Existing Link](/non-existing.md)
	% This is a commented [Non Existing Link](/non-existing.md)
	"""
)
{

	[Fact]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(3);
}
