namespace BookApiFile;

public sealed class ContentPart
{
    public bool IsBlank { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public int AnswerIndex { get; private set; } = -1;

    public static ContentPart ForText(string text) =>
        new() { Text = text };

    public static ContentPart ForBlank(int answerIndex) =>
        new() { IsBlank = true, AnswerIndex = answerIndex };
}

