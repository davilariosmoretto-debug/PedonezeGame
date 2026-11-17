using Godot;

public partial class BookStyle : Control
{
    [ExportGroup("Typography")]
    [Export] public Font ParagraphFont { get; set; }
    [Export] public int FontSize { get; set; } = 32;
    [Export] public int BlankFontSize { get; set; } = 32;
    [Export] public int TitleFontSize { get; set; } = 24;
    [Export] public Color TextColor { get; set; } = Colors.Black;
    [Export] public Color TitleTextColor { get; set; } = Colors.DarkSlateGray;


    [ExportGroup("Arc layout")]
    [Export] public float ParagraphRadius { get; set; } = 500f;
    [Export] public float MaxSweepDegrees { get; set; } = 180f;
    [Export] public float LineSpacing { get; set; } = 4f;
    [Export] public float TiltDegrees { get; set; }
    [Export] public bool FlipInward { get; set; }
    [Export] public float DegreeOffset { get; set; }

    [ExportGroup("Blank inputs")]
    [Export] public float BlankMinimumHeight { get; set; } = 40f;
    [Export] public int BlankMaxLength { get; set; } = 20;
    [Export] public float InputRadius { get; set; } = 120f;

    [ExportGroup("Pagination")]
    [Export] public float BottomMargin { get; set; } = 24f;
    [Export] public int MaxLinesPerParagraph { get; set; } = 35;

    [ExportGroup("General")]
    [Export] public AnimationPlayer AnimationPlayer { get; set; }
}

