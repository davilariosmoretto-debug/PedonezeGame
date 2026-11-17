using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

namespace BookApiFile;

public sealed class MarkdownBookLoader : IBookLoader
{
    private static readonly Regex BlockRegex = new(
        @"<(?<htmlTag>h[1-6]|p)>(?<htmlText>.*?)</\k<htmlTag>>" +
        @"|^(?<mdHashes>#{1,6})[ \t]+(?<mdText>.*?)(?:[ \t]+#+[ \t]*)?$",
        RegexOptions.Singleline |
        RegexOptions.Multiline |
        RegexOptions.IgnoreCase |
        RegexOptions.Compiled);

    private static readonly Regex BoldRegex = new(
        @"\*\*(.+?)\*\*|<b>(.*?)</b>",
        RegexOptions.Singleline |
        RegexOptions.IgnoreCase |
        RegexOptions.Compiled);

    private sealed class AnswerSource
    {
        public string FilePath { get; init; }
        public int Start { get; init; }
        public int Length { get; init; }
        public string Answer { get; init; }
    }

    private readonly Dictionary<int, AnswerSource> _answerSources = new();
    private string _activeDirectory;

    public BookDocument Load(string directory)
    {
        string sourceDirectory = string.IsNullOrWhiteSpace(directory)
            ? ProjectSettings.GlobalizePath("res://Texts/BookPages")
            : ProjectSettings.GlobalizePath(directory);

        if (!Directory.Exists(sourceDirectory))
        {
            GD.PushError($"Book pages directory does not exist: {sourceDirectory}");
            return new BookDocument();
        }

        // The source in res:// is read-only in exported builds.
        // Copy each file to user:// only if it has not already been copied.
        string userDirectory =
            ProjectSettings.GlobalizePath("user://BookPages");

        Directory.CreateDirectory(userDirectory);

        foreach (string sourceFile in Directory.EnumerateFiles(
                     sourceDirectory,
                     "*.md"))
        {
            string destination = Path.Combine(
                userDirectory,
                Path.GetFileName(sourceFile));

            bool sameFile = string.Equals(
                Path.GetFullPath(sourceFile),
                Path.GetFullPath(destination),
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);

            if (!sameFile && !File.Exists(destination))
                File.Copy(sourceFile, destination);
        }

        _activeDirectory = userDirectory;
        return LoadFromDirectory(_activeDirectory);
    }

    /// <summary>
    /// Replaces the Markdown markup for a solved answer with plain text,
    /// then reloads the book so the answer is rendered as part of its paragraph.
    /// </summary>
    public BookDocument RevealAnswer(int answerIndex)
    {
        if (!_answerSources.TryGetValue(answerIndex, out AnswerSource source))
        {
            GD.PushWarning($"No Markdown source found for answer {answerIndex}.");
            return LoadFromDirectory(_activeDirectory);
        }

        string contents = File.ReadAllText(source.FilePath);

        if (source.Start < 0 ||
            source.Start + source.Length > contents.Length)
        {
            GD.PushError(
                $"The Markdown location for answer {answerIndex} is invalid.");
            return LoadFromDirectory(_activeDirectory);
        }

        contents = contents
            .Remove(source.Start, source.Length)
            .Insert(source.Start, source.Answer);

        // source.FilePath points to the copy in user://BookPages.
        File.WriteAllText(source.FilePath, contents);

        return LoadFromDirectory(_activeDirectory);
    }

    private BookDocument LoadFromDirectory(string directory)
    {
        _answerSources.Clear();
        var document = new BookDocument();

        if (string.IsNullOrWhiteSpace(directory) ||
            !Directory.Exists(directory))
        {
            GD.PushError($"Book pages directory does not exist: {directory}");
            return document;
        }

        var files = Directory
            .EnumerateFiles(directory, "*.md")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in files)
        {
            string contents = File.ReadAllText(filePath);

            document.Pages.Add(ParsePage(
                contents,
                filePath,
                document,
                document.Pages.Count + 1));
        }

        if (document.Pages.Count == 0)
            GD.PushWarning($"No .md files found in: {directory}");

        return document;
    }

    private Page ParsePage(
        string fileContents,
        string filePath,
        BookDocument document,
        int pageNumber)
    {
        var page = new Page { Number = pageNumber };

        foreach (Match match in BlockRegex.Matches(fileContents))
        {
            bool isMarkdownHeading = match.Groups["mdHashes"].Success;

            Group content = isMarkdownHeading
                ? match.Groups["mdText"]
                : match.Groups["htmlText"];

            string tag = isMarkdownHeading
                ? $"h{match.Groups["mdHashes"].Value.Length}"
                : match.Groups["htmlTag"].Value.ToLowerInvariant();

            string rawText = content.Value;
            int leadingWhitespace =
                rawText.Length - rawText.TrimStart().Length;
            string sourceText = rawText.Trim();
            int sourceTextStart = content.Index + leadingWhitespace;

            Element element = tag == "p"
                ? new TextBlock()
                : new TitleBlock();

            ParseElementText(
                sourceText,
                sourceTextStart,
                filePath,
                element,
                document);

            page.Elements.Add(element);
        }

        return page;
    }

    private void ParseElementText(
        string sourceText,
        int sourceTextStart,
        string filePath,
        Element element,
        BookDocument document)
    {
        int textStart = 0;

        foreach (Match match in BoldRegex.Matches(sourceText))
        {
            AddTextPart(
                element,
                sourceText.Substring(textStart, match.Index - textStart));

            string answer = (
                match.Groups[1].Success
                    ? match.Groups[1].Value
                    : match.Groups[2].Value
            ).Trim();

            int answerIndex = document.Answers.Count;
            document.Answers.Add(answer);
            element.Blanks.Add(answer);
            element.Parts.Add(ContentPart.ForBlank(answerIndex));

            _answerSources[answerIndex] = new AnswerSource
            {
                FilePath = filePath,
                Start = sourceTextStart + match.Index,
                Length = match.Length,
                Answer = answer
            };

            textStart = match.Index + match.Length;
        }

        AddTextPart(element, sourceText.Substring(textStart));

        var displayText = new StringBuilder();

        foreach (ContentPart part in element.Parts)
        {
            if (!part.IsBlank)
            {
                displayText.Append(part.Text);
                continue;
            }

            string answer = part.AnswerIndex >= 0 &&
                            part.AnswerIndex < document.Answers.Count
                ? document.Answers[part.AnswerIndex]
                : string.Empty;

            int length = Math.Max(
                new System.Globalization.StringInfo(answer)
                    .LengthInTextElements,
                1);

            displayText.Append('_', length);
        }

        element.Text = displayText.ToString();
    }

    private static void AddTextPart(Element element, string text)
    {
        if (!string.IsNullOrEmpty(text))
            element.Parts.Add(ContentPart.ForText(text));
    }
}

