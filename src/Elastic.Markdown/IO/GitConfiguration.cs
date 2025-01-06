// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json.Serialization;
using SoftCircuits.IniFileParser;

namespace Elastic.Markdown.IO;

public record GitConfiguration
{
	[JsonPropertyName("branch")] public required string Branch { get; init; }
	[JsonPropertyName("remote")] public required string Remote { get; init; }
	[JsonPropertyName("ref")] public required string Ref { get; init; }

	// manual read because libgit2sharp is not yet AOT ready
	public static GitConfiguration Create(IFileSystem fileSystem)
	{
		// filesystem is not real so return a dummy
		if (fileSystem is not FileSystem)
		{
			var fakeRef = Guid.NewGuid().ToString().Substring(0, 16);
			return new GitConfiguration { Branch = $"test-{fakeRef}", Remote = "elastic/docs-builder", Ref = fakeRef, };
		}

		var gitConfig = Git(".git/config");
		if (!gitConfig.Exists)
			throw new Exception($"{Paths.Root.FullName} is not a git repository.");

		var head = Read(".git/HEAD");
		var gitRef = head;
		var branch = head.Replace("refs/heads/", string.Empty);
		//not detached HEAD
		if (head.StartsWith("ref:"))
		{
			head = head.Replace("ref: ", string.Empty);
			gitRef = Read(".git/" + head);
			branch = branch.Replace("ref: ", string.Empty);
		}
		else
			branch = "detached/head";

		var ini = new IniFile();
		using var stream = gitConfig.OpenRead();
		using var streamReader = new StreamReader(stream);
		ini.Load(streamReader);

		var remote = BranchTrackingRemote(branch, ini);
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote("main", ini);
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote("master", ini);
		if (string.IsNullOrEmpty(remote))
			remote = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") ?? "elastic/docs-builder-unknown";

		return new GitConfiguration { Ref = gitRef, Branch = branch, Remote = remote };

		IFileInfo Git(string path) => fileSystem.FileInfo.New(Path.Combine(Paths.Root.FullName, path));

		string Read(string path) =>
			fileSystem.File.ReadAllText(Git(path).FullName).Trim(Environment.NewLine.ToCharArray());

		string BranchTrackingRemote(string b, IniFile c)
		{
			var sections = c.GetSections();
			var branchSection = $"branch \"{b}\"";
			if (!sections.Contains(branchSection))
				return string.Empty;

			var remoteName = ini.GetSetting(branchSection, "remote");

			var remoteSection = $"remote \"{remoteName}\"";

			remote = ini.GetSetting(remoteSection, "url");
			return remote ?? string.Empty;
		}
	}
}
