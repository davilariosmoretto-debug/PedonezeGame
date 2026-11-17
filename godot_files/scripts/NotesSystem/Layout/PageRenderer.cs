using System;
using System.Collections.Generic;
using BookApiFile;
using Godot;
using ParagraphFile;

namespace BookLayout;

public sealed class PageRenderer
{
    private readonly BoxContainer _leftPage;
    private readonly BoxContainer _rightPage;
    private readonly Control _leftClip;
    private readonly Control _rightClip;
    private readonly BookStyle _style;
    private readonly IReadOnlyList<string> _answers;

    public event Action<int> AnswerSolved;

    public PageRenderer(
        BoxContainer leftPage,
        BoxContainer rightPage,
        Control leftClip,
        Control rightClip,
        BookStyle style,
        IReadOnlyList<string> answers)
    {
        _leftPage = leftPage;
        _rightPage = rightPage;
        _leftClip = leftClip;
        _rightClip = rightClip;
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _answers = answers ?? Array.Empty<string>();
    }

    public void Render(
        IReadOnlyList<List<RenderItem>> pages,
        int leftPageIndex,
        float leftTilt,
        float rightTilt)
    {
        RenderOne(_leftPage, _leftClip, pages, leftPageIndex, leftTilt);
        RenderOne(_rightPage, _rightClip, pages, leftPageIndex + 1, rightTilt);
    }

    private void RenderOne(
        BoxContainer page,
        Control clip,
        IReadOnlyList<List<RenderItem>> pages,
        int pageIndex,
        float tilt)
    {
        Clear(page);

        if (pageIndex < 0 || pageIndex >= pages.Count)
            return;

        foreach (RenderItem item in pages[pageIndex])
        {
            bool isTitle = item.IsTitle;

            var paragraph = new Paragraph
            {
                Text = item.Text,
                FontStyle = _style.ParagraphFont,
                FontSize = isTitle ? _style.TitleFontSize : _style.FontSize,
                Radius = _style.ParagraphRadius,
                MaxSweepDegrees = _style.MaxSweepDegrees,
                LineSpacing = _style.LineSpacing,
                TextColor = isTitle ? _style.TitleTextColor : _style.TextColor,
                FlipInward = _style.FlipInward,
                DegreeOffset = _style.DegreeOffset,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TiltDegrees = tilt,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            page.AddChild(paragraph);

            var overlay = new BlankInputOverlay
            {
                Paragraph = paragraph,
                AnswerIndices = item.AnswerIndices,
                Answers = _answers,
                Style = _style,
                OnAnswerSolved = answerIndex => AnswerSolved?.Invoke(answerIndex)
            };

            paragraph.AddChild(overlay);
        }

        ApplyClipWidth(clip, page);
    }

    private static void Clear(BoxContainer page)
    {
        foreach (Node child in page.GetChildren())
        {
            page.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static void ApplyClipWidth(Control clip, BoxContainer page)
    {
        float width = clip.Size.X;
        if (width <= 0f)
            return;

        foreach (Node child in page.GetChildren())
        {
            if (child is Control control)
            {
                control.CustomMinimumSize = new Vector2(
                    width,
                    control.CustomMinimumSize.Y);
            }
        }
    }
}

