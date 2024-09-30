using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.DocSet;

public class DocumentationGroup
{
	public MarkdownFile? Index { get; }
	public MarkdownFile[] Files { get; }
	public OrderedList<MarkdownFile> FilesInOrder { get; set; }
	public DocumentationGroup[] Nested { get; }
	public OrderedList<DocumentationGroup> GroupsInOrder { get; set; }
	public int Level { get; }
	public string? FolderName { get; }

	public bool Current { get; internal set; }
	public MarkdownFile? CurrentFile { get; internal set; }

	public DocumentationGroup(Dictionary<string, MarkdownFile[]> markdownFiles, int level, string folderName)
	{
		Level = level;
		FolderName = folderName;

		var files = markdownFiles
			.Where(k => k.Key.EndsWith(".md")).SelectMany(g => g.Value)
			.Where(file => file.ParentFolders.Count == level)
			.ToArray();


		Files = files
			.Where(file => file.FileName != "index.md")
			.ToArray();

		FilesInOrder = new OrderedList<MarkdownFile>(Files);

		Index = files.FirstOrDefault(f => f.FileName == "index.md");

		var newLevel = level + 1;
		var groups = new List<DocumentationGroup>();
		foreach (var kv in markdownFiles.Where(kv=> !kv.Key.EndsWith(".md")))
		{
			var folder = kv.Key;
			var folderFiles = kv.Value
				.Where(file => file.ParentFolders.Count > level)
				.Where(file => file.ParentFolders[level] == folder).ToArray();
			var mapped = folderFiles
				.GroupBy(file =>
				{
					var path = file.ParentFolders.Count > newLevel ? file.ParentFolders[newLevel] : file.FileName;
					return path;
				})
				.ToDictionary(k => k.Key, v => v.ToArray());
			var documentationGroup  = new DocumentationGroup(mapped, newLevel, folder);
			groups.Add(documentationGroup);

		}
		Nested = groups.ToArray();
		GroupsInOrder = new OrderedList<DocumentationGroup>(Nested);
	}

	private bool HoldsCurrent(MarkdownFile current) =>
		Files.Contains(current) || Nested.Any(n => n.HoldsCurrent(current));

	public async Task Resolve(MarkdownFile markdown, CancellationToken ctx = default)
	{
		CurrentFile = markdown;

		await (Index?.ParseAsync(ctx) ?? Task.CompletedTask);
		foreach (var f in Files) await f.ParseAsync(ctx);
		foreach (var n in Nested) await n.Resolve(markdown, ctx);

		Current = HoldsCurrent(markdown);

		if (Index?.TocTree == null)
			return;

		var tree = Index.TocTree;
		var fileList = new OrderedList<MarkdownFile>();
		var groupList = new OrderedList<DocumentationGroup>();

		foreach (var link in tree)
		{
			var file = Files.FirstOrDefault(f => f.RelativePath.EndsWith(link.Link));
			if (file != null)
			{
				file.TocTitle = link.Title;
				fileList.Add(file);
			}
			else
			{

			}

			var group = Nested.FirstOrDefault(f => f.Index != null && f.Index.RelativePath.EndsWith(link.Link));
			if (group != null)
			{
				groupList.Add(group);
				if (group.Index != null && !string.IsNullOrEmpty(link.Title))
					group.Index.TocTitle = link.Title;
			}

			//TODO LOG ERROR
		}

		FilesInOrder = fileList;
		GroupsInOrder = groupList;
	}
}

public class DocumentationSet
{
	public string Name { get; }
	public DirectoryInfo SourcePath { get; }
	public DirectoryInfo OutputPath { get; }

	private MarkdownConverter MarkdownConverter { get; }

	public DocumentationSet(string name, DirectoryInfo sourcePath, DirectoryInfo outputPath, MarkdownConverter markdownConverter)
	{
		Name = name;
		SourcePath = sourcePath;
		OutputPath = outputPath;
		MarkdownConverter = markdownConverter;

		Files = Directory.EnumerateFiles(SourcePath.FullName, "*.*", SearchOption.AllDirectories)
			.Select(f => new FileInfo(f))
			.Select<FileInfo, DocumentationFile>(file => file.Extension switch
			{
				".png" => new ImageFile(file, SourcePath, OutputPath),
				".md" => new MarkdownFile(file, SourcePath, OutputPath)
				{
					MarkdownConverter = MarkdownConverter
				},
				_ => new StaticFile(file, SourcePath, OutputPath)
			})
			.ToList();

		FlatMappedFiles = Files.ToDictionary(file => file.RelativePath, file => file);

		var markdownFiles = Files.OfType<MarkdownFile>()
			.Where(file => !file.RelativePath.StartsWith("_"))
			.GroupBy(file =>
			{
				var path = file.ParentFolders.Count >= 1 ? file.ParentFolders[0] : file.FileName;
				return path;
			})
			.ToDictionary(k => k.Key, v => v.ToArray());

		Tree = new DocumentationGroup(markdownFiles, 0, "");
	}

	public DocumentationGroup Tree { get; }

	public List<DocumentationFile> Files { get; }
	public Dictionary<string, DocumentationFile> FlatMappedFiles { get; }

	public void ClearOutputDirectory()
	{
		if (OutputPath.Exists)
			OutputPath.Delete(true);
		OutputPath.Create();
	}
}
