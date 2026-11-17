using System.Collections.Generic;

namespace BookApiFile;

public sealed class BookDocument
{
    public List<Page> Pages { get; } = new();
    public List<string> Answers { get; } = new();
}

public sealed class Page
{
    public int Number { get; set; }
    public List<Element> Elements { get; set; } = new();
}

public abstract class Element
{
    public string Text { get; set; } = string.Empty;
    public List<string> Blanks { get; } = new();
    public List<ContentPart> Parts { get; } = new();
}

public sealed class TextBlock : Element { }
public sealed class TitleBlock : Element { }

