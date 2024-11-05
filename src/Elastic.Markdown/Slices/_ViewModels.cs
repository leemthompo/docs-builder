using Elastic.Markdown.IO;

namespace Elastic.Markdown.Slices;

public class IndexViewModel
{
	public required string Title { get; init; }
	public required string MarkdownHtml { get; init; }
	public required DocumentationFolder Tree { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required string Navigation { get; init; }
	public required string? UrlPathPrefix { get; init; }
}

public class LayoutViewModel
{
	public string Title { get; set; } = "Elastic Documentation";
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required DocumentationFolder Tree { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required string Navigation { get; set; }
	public required string? UrlPathPrefix { get; set; }


	public string Static(string path)
	{
		path = $"_static/{path.TrimStart('/')}";
		return $"{UrlPathPrefix}/{path}";
	}

	public string Link(string path)
	{
		path = path.TrimStart('/');
		return $"{UrlPathPrefix}/{path}";
	}
}

public class PageTocItem
{
	public required string Heading { get; init; }
	public required string Slug { get; init; }
}


public class NavigationViewModel
{
	public required DocumentationFolder Tree { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
}

public class NavigationTreeItem
{
	public required int Level { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required DocumentationFolder SubTree { get; init; }
}
