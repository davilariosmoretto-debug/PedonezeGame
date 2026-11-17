using System.Collections.Generic;

namespace BookLayout;

public sealed class RenderItem
{
    public string Text { get; init; } = string.Empty;
    public bool IsTitle { get; set; }
    public List<int> AnswerIndices { get; init; } = new();
}

