using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BookApiFile;
using Godot;
using ParagraphFile;

namespace BookLayout;

public sealed class PagePaginator
{
    private readonly BookStyle _style;

    private static readonly Regex TokenRegex = new(@"\S+\s*");
    private static readonly Regex BlankRegex = new(@"_+");

    public PagePaginator(BookStyle style)
    {
        _style = style ?? throw new ArgumentNullException(nameof(style));
    }

    private Paragraph CreateParagraph(
        string text,
        float width,
        bool isTitle = false)
    {
        var paragraph = new Paragraph
        {
            Size = new Vector2(width, 0f),
            Text = text,
            FontStyle = _style.ParagraphFont,
            FontSize = isTitle ? _style.TitleFontSize : _style.FontSize,
            TextColor = isTitle ? _style.TitleTextColor : _style.TextColor,
            Radius = _style.ParagraphRadius,
            MaxSweepDegrees = _style.MaxSweepDegrees,
            LineSpacing = _style.LineSpacing
        };

        paragraph.RefreshLayout();
        return paragraph;
    }

    public float MeasureTextHeight(string text, float width)
    {
        return MeasureTextHeight(text, width, isTitle: false);
    }

    private float MeasureTextHeight(string text, float width, bool isTitle)
    {
        Paragraph paragraph = CreateParagraph(text, width, isTitle);
        float height = paragraph.GetLayoutHeight();
        paragraph.Free();
        return height;
    }

    private int MeasureWrappedLineCount(
        string text,
        float width,
        bool isTitle)
    {
        Paragraph paragraph = CreateParagraph(text, width, isTitle);
        int lineCount = paragraph.WrappedLineCount;
        paragraph.Free();
        return lineCount;
    }

    private IEnumerable<RenderItem> SplitToFit(
        RenderItem item,
        float pageWidth,
        float firstCapacity,
        float fullCapacity)
    {
        var currentText = new StringBuilder();
        var currentAnswers = new List<int>();
        int answerIndex = 0;
        float capacity = firstCapacity;

        RenderItem MakeFragment() => new()
        {
            Text = currentText.ToString().TrimEnd(),
            IsTitle = item.IsTitle,
            AnswerIndices = new List<int>(currentAnswers)
        };

        void AddAnswers(string text)
        {
            int blanks = BlankRegex.Matches(text).Count;

            for (int i = 0;
                 i < blanks && answerIndex < item.AnswerIndices.Count;
                 i++)
            {
                currentAnswers.Add(item.AnswerIndices[answerIndex++]);
            }
        }

        void ResetForNextFragment()
        {
            currentText.Clear();
            currentAnswers = new List<int>();
            capacity = fullCapacity;
        }

        foreach (Match match in TokenRegex.Matches(item.Text))
        {
            string token = match.Value;
            string candidate = (currentText.ToString() + token).TrimEnd();

            // Keep the token (word and following whitespace) together.
            // Ignore trailing whitespace while measuring.
            if (currentText.Length > 0 &&
                MeasureTextHeight(candidate, pageWidth, item.IsTitle) > capacity)
            {
                yield return MakeFragment();
                ResetForNextFragment();
            }

            // Append the whole token, even if it alone exceeds the capacity.
            // This prevents splitting a word mid-word.
            currentText.Append(token);
            AddAnswers(token);
        }

        if (currentText.Length > 0)
            yield return MakeFragment();
    }

    private IEnumerable<RenderItem> SplitByLineCount(
        RenderItem item,
        float pageWidth,
        int maxLines)
    {
        maxLines = Math.Max(1, maxLines);

        var currentText = new StringBuilder();
        var currentAnswers = new List<int>();
        int answerIndex = 0;

        RenderItem MakeFragment()
        {
            return new RenderItem
            {
                Text = currentText.ToString().TrimEnd(),
                IsTitle = item.IsTitle,
                AnswerIndices = new List<int>(currentAnswers)
            };
        }

        void AddAnswers(string text)
        {
            int blankCount = BlankRegex.Matches(text).Count;

            for (int i = 0;
                 i < blankCount && answerIndex < item.AnswerIndices.Count;
                 i++)
            {
                currentAnswers.Add(item.AnswerIndices[answerIndex++]);
            }
        }

        foreach (Match match in TokenRegex.Matches(item.Text))
        {
            string token = match.Value;
            string candidate = (currentText.ToString() + token).TrimEnd();

            // If adding this whole token exceeds the limit, start a new
            // fragment first. Never split the token into individual characters.
            if (currentText.Length > 0 &&
                MeasureWrappedLineCount(candidate, pageWidth, item.IsTitle) > maxLines)
            {
                yield return MakeFragment();
                currentText.Clear();
                currentAnswers = new List<int>();
            }

            // Keep the entire word together, even if it alone exceeds maxLines.
            currentText.Append(token);
            AddAnswers(token);
        }

        if (currentText.Length > 0)
            yield return MakeFragment();
    }

    public List<List<RenderItem>> Paginate(
        BookDocument document,
        float pageWidth,
        float pageHeight,
        float elementGap)
    {
        var pages = new List<List<RenderItem>>();
        var currentPage = new List<RenderItem>();

        float usableHeight = Mathf.Max(1f, pageHeight - _style.BottomMargin);
        float usedHeight = 0f;

        void StartNextPage()
        {
            if (currentPage.Count > 0)
                pages.Add(currentPage);

            currentPage = new List<RenderItem>();
            usedHeight = 0f;
        }

        foreach (Page sourcePage in document.Pages)
        {
            foreach (Element element in sourcePage.Elements)
            {
                var item = new RenderItem
                {
                    Text = element.Text,
                    IsTitle = element is TitleBlock,
                    AnswerIndices = element.Parts
                        .Where(part => part.IsBlank)
                        .Select(part => part.AnswerIndex)
                        .ToList()
                };

                foreach (RenderItem lineChunk in SplitByLineCount(
                    item,
                    pageWidth,
                    _style.MaxLinesPerParagraph))
                {
                    float gap = currentPage.Count > 0 ? elementGap : 0f;
                    float remaining = Mathf.Max(
                        1f,
                        usableHeight - usedHeight - gap);

                    foreach (RenderItem fragment in SplitToFit(
                        lineChunk,
                        pageWidth,
                        remaining,
                        usableHeight))
                    {
                        float height = MeasureTextHeight(
                            fragment.Text,
                            pageWidth,
                            fragment.IsTitle);

                        gap = currentPage.Count > 0 ? elementGap : 0f;

                        if (currentPage.Count > 0 &&
                            usedHeight + gap + height > usableHeight)
                        {
                            StartNextPage();
                            gap = 0f;
                        }

                        currentPage.Add(fragment);
                        usedHeight += gap + height;
                    }
                }
            }
        }

        if (currentPage.Count > 0)
            pages.Add(currentPage);

        return pages;
    }
}

