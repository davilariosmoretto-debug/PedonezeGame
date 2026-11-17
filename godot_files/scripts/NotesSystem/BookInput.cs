using Godot;
using System;
using System.Globalization;

public partial class BookInput : Control
{
    public BookStyle Style { get; set; }
    public Action<int> OnAnswerSolved { get; set; }

    private bool _wasSolved;
    private bool _caretOn = true;
    private double _blink;

    // Per-input arc geometry
    public float Radius { get; set; } = -1f;
    public float StartDegrees { get; set; }
    public float SweepDegrees { get; set; } = 180f;
    public int MaxLengthOverride { get; set; }
    public Vector2 ArcCenter { get; set; } = new(-1f, -1f);

    public int Index { get; set; }
    public string Text { get; private set; } = string.Empty;
    public string ExpectedText { get; set; } = string.Empty;

    private float EffectiveRadius =>
        Radius > 0f ? Radius : Style?.InputRadius ?? 120f;

    private int EffectiveFontSize =>
        Style?.BlankFontSize ?? 32;

    private int EffectiveMaxLength =>
        MaxLengthOverride > 0 ? MaxLengthOverride : Style?.BlankMaxLength ?? 20;

    private Color EffectiveTextColor =>
        Style?.TextColor ?? Colors.Black;

    private int CharacterCount =>
        new StringInfo(Text).LengthInTextElements;

    public override void _Process(double delta)
    {
        if (!HasFocus() || _wasSolved)
            return;

        _blink += delta;
        if (_blink >= 0.5)
        {
            _blink = 0;
            _caretOn = !_caretOn;
            QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse &&
            mouse.Pressed &&
            mouse.ButtonIndex == MouseButton.Left)
        {
            GrabFocus();
            _caretOn = true;
            _blink = 0;
            SubmitAnswer();
            QueueRedraw();
            AcceptEvent();
            return;
        }

        if (!HasFocus() ||
            _wasSolved ||
            @event is not InputEventKey key ||
            !key.Pressed ||
            key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter)
        {
            SubmitAnswer();
            AcceptEvent();
            return;
        }

        if (key.Keycode == Key.Backspace)
        {
            RemoveLastTextElement();
        }
        else if (key.Unicode >= 32 && CharacterCount < EffectiveMaxLength)
        {
            Text += char.ConvertFromUtf32((int)key.Unicode);
        }
        else
        {
            return;
        }

        _caretOn = true;
        _blink = 0;
        QueueRedraw();
        AcceptEvent();
    }

    private void SubmitAnswer()
    {
        if (_wasSolved || !IsCorrect())
            return;

        _wasSolved = true;
        QueueRedraw();
        OnAnswerSolved?.Invoke(Index);
    }

    private void RemoveLastTextElement()
    {
        if (Text.Length == 0)
            return;

        var info = new StringInfo(Text);
        Text = info.SubstringByTextElements(
            0,
            info.LengthInTextElements - 1);
    }

    public override void _Draw()
    {
        Font font = Style?.ParagraphFont ?? ThemeDB.FallbackFont;
        int fontSize = EffectiveFontSize;
        int slots = Mathf.Max(EffectiveMaxLength, 1);
        float radius = EffectiveRadius;
        Color textColor = EffectiveTextColor;
        Vector2 center = ArcCenter.X >= 0f ? ArcCenter : Size / 2f;

        string[] characters = new string[CharacterCount];
        var enumerator = StringInfo.GetTextElementEnumerator(Text);
        int characterIndex = 0;

        while (enumerator.MoveNext())
            characters[characterIndex++] = enumerator.GetTextElement();

        for (int i = 0; i < slots; i++)
        {
            bool typed = i < characters.Length;

            // Don't draw unused placeholder marks after a correct answer.
            if (_wasSolved && !typed)
                continue;

            float t = (i + 0.5f) / slots;
            float angle = Mathf.DegToRad(StartDegrees + SweepDegrees * t);
            Vector2 position = center +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            string glyph = typed ? characters[i] : "_";
            Color color = typed
                ? textColor
                : new Color(textColor, 0.4f);

            float glyphWidth = font.GetStringSize(
                glyph,
                HorizontalAlignment.Left,
                -1f,
                fontSize).X;

            DrawSetTransform(position, angle + Mathf.Pi / 2f);
            DrawString(
                font,
                new Vector2(-glyphWidth / 2f, 0f),
                glyph,
                HorizontalAlignment.Left,
                -1f,
                fontSize,
                color);
        }

        DrawSetTransform(Vector2.Zero, 0f);

        // Draw the caret while answering; hide it once the answer is correct.
        if (HasFocus() && !_wasSolved && _caretOn && characters.Length < slots)
        {
            float t = (characters.Length + 0.5f) / slots;
            float angle = Mathf.DegToRad(StartDegrees + SweepDegrees * t);
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));

            DrawLine(
                center + direction * (radius - fontSize / 2f),
                center + direction * (radius + fontSize / 2f),
                textColor,
                2f);
        }
    }

    public bool IsCorrect()
    {
        return string.Equals(
            Text.Trim(),
            ExpectedText.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        Text = string.Empty;
        _wasSolved = false;
        _caretOn = true;
        _blink = 0;
        QueueRedraw();
    }
}

