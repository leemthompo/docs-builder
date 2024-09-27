// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.CustomContainers;

/// <summary>
/// A block custom container.
/// </summary>
/// <seealso cref="ContainerBlock" />
/// <seealso cref="IFencedBlock" />
public class Admonition : ContainerBlock, IFencedBlock
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Admonition"/> class.
	/// </summary>
	/// <param name="parser">The parser used to create this block.</param>
	/// <param name="admonitionData"></param>
	public Admonition(AdmonitionParser parser, Dictionary<string, string> admonitionData) : base(parser) =>
	    AdmonitionData = admonitionData;

    public IReadOnlyDictionary<string, string> AdmonitionData { get; }

    /// <inheritdoc />
    public char FencedChar { get; set; }

    /// <inheritdoc />
    public int OpeningFencedCharCount { get; set; }

    /// <inheritdoc />
    public StringSlice TriviaAfterFencedChar { get; set; }

    /// <inheritdoc />
    public string? Info { get; set; }

    /// <inheritdoc />
    public StringSlice UnescapedInfo { get; set; }

    /// <inheritdoc />
    public StringSlice TriviaAfterInfo { get; set; }

    /// <inheritdoc />
    public string? Arguments { get; set; }

    /// <inheritdoc />
    public StringSlice UnescapedArguments { get; set; }

    /// <inheritdoc />
    public StringSlice TriviaAfterArguments { get; set; }

    /// <inheritdoc />
    public NewLine InfoNewLine { get; set; }

    /// <inheritdoc />
    public StringSlice TriviaBeforeClosingFence { get; set; }

    /// <inheritdoc />
    public int ClosingFencedCharCount { get; set; }
}
