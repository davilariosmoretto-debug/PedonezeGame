using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using ParagraphFile;

namespace BookLayout;

public partial class BlankInputOverlay : Control
{
    public IReadOnlyList<int> AnswerIndices { get; set; } = Array.Empty<int>();
    public IReadOnlyList<string> Answers { get; set; } = Array.Empty<string>();
    public Func<int, int> MaxLengthForAnswer { get; set; }
    public Paragraph Paragraph { get; set; }
    public BookStyle Style { get; set; }
    public Action<int> OnAnswerSolved { get; set; }

    private bool _rebuildQueued;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        if (Paragraph != null)
            Paragraph.Resized += QueueRebuild;

        QueueRebuild();
    }

    private void QueueRebuild()
    {
        if (_rebuildQueued || !IsInsideTree())
            return;

        _rebuildQueued = true;
        CallDeferred(nameof(RebuildInputs));
    }

    public event Action<int, string> AnswerSolved;

    private void HandleAnswerSolved(int answerIndex, string answer)
    {
        AnswerSolved?.Invoke(answerIndex, answer);
    }


    private void RebuildInputs()
    {
        _rebuildQueued = false;

        if (Paragraph == null || !IsInsideTree())
            return;

        Paragraph.RefreshLayout();

        foreach (Node child in GetChildren())
            child.QueueFree();

        int fontSize = Style?.BlankFontSize ?? 32;

        for (int i = 0; i < AnswerIndices.Count; i++)
        {
            Paragraph.SegmentRect segment = Paragraph.GetSegmentRect(i);
            if (segment.Radius <= 0f)
                continue;

            int answerIndex = AnswerIndices[i];
            Rect2 bounds = GetArcBounds(segment, fontSize);

            string answer =
                answerIndex >= 0 && answerIndex < Answers.Count
                    ? Answers[answerIndex]
                    : string.Empty;

            int maxLength = MaxLengthForAnswer?.Invoke(answerIndex)
                ?? GetTextElementCount(answer);

            maxLength = Math.Max(maxLength, 1);

            var input = new BookInput
            {
                Style = Style,
                Index = answerIndex,
                ExpectedText = answer,
                MaxLengthOverride = maxLength,
                Radius = segment.Radius,
                StartDegrees = segment.StartDegrees,
                SweepDegrees = segment.SweepDegrees,
                ArcCenter = segment.Center - bounds.Position,
                Position = bounds.Position,
                Size = bounds.Size,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.All,
                OnAnswerSolved = answerIndex => OnAnswerSolved?.Invoke(answerIndex),
            };

            AddChild(input);
        }
    }

    private static int GetTextElementCount(string text)
    {
        return new StringInfo(text).LengthInTextElements;
    }

    private static Rect2 GetArcBounds(
        Paragraph.SegmentRect segment,
        int fontSize)
    {
        float start = Mathf.DegToRad(segment.StartDegrees);
        float sweep = Mathf.DegToRad(segment.SweepDegrees);
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        const int samples = 32;

        for (int i = 0; i <= samples; i++)
        {
            float angle = start + sweep * i / samples;
            Vector2 point = segment.Center +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * segment.Radius;

            minX = Mathf.Min(minX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxX = Mathf.Max(maxX, point.X);
            maxY = Mathf.Max(maxY, point.Y);
        }

        float pad = fontSize * 1.5f;
        Vector2 topLeft = new(minX - pad, minY - pad);
        Vector2 size = new(maxX - minX + pad * 2f, maxY - minY + pad * 2f);

        return new Rect2(topLeft, size);
    }
}

