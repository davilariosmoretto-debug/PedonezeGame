using Godot;
using System;
using BookLayout;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ParagraphFile
{
    public partial class Paragraph : Control
    {
        private string _text;
        private Font _fontStyle;
        private int _fontSize = 16;
        private Color _textColor = Colors.Black;
        private bool _flipInward;
        private float _radius = 200f;
        private float _degreeOffset;
        private float _tiltDegrees;
        private float _maxSweepDegrees = 180f;
        private TextServer.AutowrapMode _autowrapMode = TextServer.AutowrapMode.Off;
        private HorizontalAlignment _alignment = HorizontalAlignment.Left;
        private float _lineSpacing = 4f;

        private static readonly Regex BlankRegex =
            new Regex(@"_+(?: _+)*", RegexOptions.Compiled);

        public override void _Ready() {
            Resized += MarkDirty;
            ApplyStyle();
        }

        private void ApplyStyle() {
            if (Style == null)
                return;

            FontStyle = Style.ParagraphFont;
            FontSize = Style.FontSize;
            TextColor = Style.TextColor;
            FlipInward = Style.FlipInward;
            Radius = Style.ParagraphRadius;
            DegreeOffset = Style.DegreeOffset;
            TiltDegrees = Style.TiltDegrees;
            MaxSweepDegrees = Style.MaxSweepDegrees;
            LineSpacing = Style.LineSpacing;
        }

        [Export] public BookStyle Style { get; set; }

        [Export]
        public string Text {
            get => _text; 
            set {
                if (_text == value)
                    return;

                _text = value;
                MarkDirty();
            }
        }

        [Export]
        public Font FontStyle {
            get => _fontStyle;
            set {
                if (_fontStyle == value)
                    return;

                _fontStyle = value;
                MarkDirty();
            }
        }

        [Export]
        public int FontSize {
            get => _fontSize;
            set {
                if (_fontSize == value)
                    return;

                _fontSize = value;
                MarkDirty();
            }
        }

        [Export]
        public float Radius {
            get => _radius;
            set {
                if (Mathf.IsEqualApprox(_radius, value))
                    return;

                _radius = value;
                MarkDirty();
            }
        }

        [Export]
        public float MaxSweepDegrees {
            get => _maxSweepDegrees;
            set {
                if (Mathf.IsEqualApprox(_maxSweepDegrees, value))
                    return;

                _maxSweepDegrees = value;
                MarkDirty();
            }
        }

        [Export]
        public TextServer.AutowrapMode AutowrapMode {
            get => _autowrapMode;
            set {
                if (_autowrapMode == value)
                    return;

                _autowrapMode = value;
                MarkDirty();
            }
        }

        [Export]
        public HorizontalAlignment Alignment {
            get => _alignment;
            set {
                if (_alignment == value)
                    return;

                _alignment = value;
                MarkDirty();
            }
        }

        [Export]
        public float LineSpacing {
            get => _lineSpacing;
            set {
                if (Mathf.IsEqualApprox(_lineSpacing, value))
                    return;

                _lineSpacing = value;
                MarkDirty();
            }
        }

        [Export]
        public Color TextColor {
            get => _textColor;
            set {
                if (_textColor == value)
                    return;

                _textColor = value;
                QueueRedraw();
            }
        }

        [Export]
        public bool FlipInward {
            get => _flipInward;
            set {
                if (_flipInward == value)
                    return;

                _flipInward = value;
                MarkDirty();
            }
        }

        [Export]
        public float DegreeOffset {
            get => _degreeOffset;
            set {
                if (Mathf.IsEqualApprox(_degreeOffset, value))
                    return;

                _degreeOffset = value;
                MarkDirty();
            }
        }

        [Export]
        public float TiltDegrees {
            get => _tiltDegrees;
            set {
                if (Mathf.IsEqualApprox(_tiltDegrees, value))
                    return;

                _tiltDegrees = value;
                QueueRedraw();
            }
        }

        public string WrappedText { get; private set; } = "";

        /// <summary>
        /// Number of lines produced by the current layout.
        /// Accessing this property ensures the layout is up to date.
        /// </summary>
        public int WrappedLineCount
        {
            get
            {
                EnsureLayout();
                return _lines.Count;
            }
        }

        public readonly struct SegmentRect
        {
            public readonly float StartDegrees;
            public readonly float SweepDegrees;
            public readonly Vector2 Center;
            public readonly float Radius;

            public SegmentRect(
                float startDegrees,
                float sweepDegrees,
                Vector2 center,
                float radius) {
                StartDegrees = startDegrees;
                SweepDegrees = sweepDegrees;
                Center = center;
                Radius = radius;
            }
        }

        private struct ArcLine
        {
            public string Text;
            public float Radius;
        }

        private struct Glyph
        {
            public Vector2 Pos;
            public float Rotation;
            public float Angle;
            public float Radius;
            public int LineIndex;
            public char Ch;
            public float Advance;
        }

        private List<ArcLine> _lines = new List<ArcLine>();
        private List<Glyph> _glyphs = new List<Glyph>();
        private string _glyphText = "";
        private List<(int Start, int Count)> _blankRuns =
            new List<(int, int)>();

        private float _outerRadius;
        private float _bboxCenterX;
        private float _bboxMinY;
        private float _bboxHeight;
        private bool _layoutDirty = true;

        public void RefreshLayout() {
            MarkDirty();
        }

        public SegmentRect GetSegmentRect(int localIndex) {
            EnsureLayout();

            if (localIndex < 0 || localIndex >= _blankRuns.Count)
                return new SegmentRect(0f, 0f, GetArcCenter(), 0f);

            (int start, int count) = _blankRuns[localIndex];
            Glyph first = _glyphs[start];

            int last = start;
            while (last + 1 < start + count &&
                   _glyphs[last + 1].LineIndex == first.LineIndex) {
                last++;
            }

            Glyph lastGlyph = _glyphs[last];
            float radius = first.Radius;

            if (radius <= 0f)
                return new SegmentRect(0f, 0f, GetArcCenter(), 0f);

            float startAngle =
                first.Angle - (first.Advance / 2f) / radius;
            float endAngle =
                lastGlyph.Angle + (lastGlyph.Advance / 2f) / radius;

            float tilt = Mathf.DegToRad(TiltDegrees);
            float inputStart = startAngle + tilt - Mathf.Pi / 2f;
            float sweep = endAngle - startAngle;

            return new SegmentRect(
                Mathf.RadToDeg(inputStart),
                Mathf.RadToDeg(sweep),
                GetArcCenter(),
                radius);
        }

        private void MarkDirty() {
            _layoutDirty = true;
            UpdateMinimumSize();
            QueueRedraw();
        }

        public override Vector2 _GetMinimumSize() {
            EnsureLayout();
            return new Vector2(0f, _bboxHeight);
        }
        // Paragraph.cs
        public float GetLayoutHeight() {
            EnsureLayout();
            return _bboxHeight;
        }


        private void EnsureLayout() {
            if (!_layoutDirty)
                return;

            _layoutDirty = false;
            BuildLayout();
        }

        private float LineHeight => FontSize + LineSpacing;
        private float MinRadius => FontSize * 1.5f;

        private void BuildLayout() {
            _lines = new List<ArcLine>();
            _glyphs = new List<Glyph>();
            _glyphText = "";
            _blankRuns = new List<(int, int)>();
            WrappedText = "";
            _outerRadius = Radius;
            _bboxHeight = 0f;

            if (FontStyle == null ||
                string.IsNullOrEmpty(Text) ||
                FontSize <= 0 ||
                Radius <= 0f) {
                return;
            }

            float availableWidth = Size.X > 0f ? Size.X : Radius * 2f;
            float halfWidth = Mathf.Max(
                availableWidth / 2f - FontSize,
                FontSize);

            List<string> wrapped = null;

            for (int attempt = 0; attempt < 20; attempt++) {
                wrapped = WrapAll(_outerRadius, halfWidth);

                float innerRadius =
                    _outerRadius - (wrapped.Count - 1) * LineHeight;

                if (innerRadius >= MinRadius)
                    break;

                _outerRadius += MinRadius - innerRadius;
            }

            for (int i = 0; i < wrapped.Count; i++) {
                _lines.Add(new ArcLine
                {
                    Text = wrapped[i],
                    Radius = _outerRadius - i * LineHeight
                });
            }

            WrappedText = string.Join("\n", wrapped);
            BuildGlyphs();
            BuildBlankRuns();
        }

        private void BuildGlyphs() {
            float baseAngle = Mathf.DegToRad(DegreeOffset);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            var glyphText = new System.Text.StringBuilder();

            for (int lineIndex = 0; lineIndex < _lines.Count; lineIndex++) {
                ArcLine arcLine = _lines[lineIndex];

                if (string.IsNullOrEmpty(arcLine.Text))
                    continue;

                float radius = arcLine.Radius;
                float lineSweep = MeasureWidth(arcLine.Text) / radius;
                float angle = baseAngle - lineSweep / 2f;

                foreach (char ch in arcLine.Text) {
                    float advance = FontStyle.GetCharSize(ch, FontSize).X;
                    float glyphAngle = angle + (advance / 2f) / radius;

                    Vector2 pos =
                        new Vector2(Mathf.Sin(glyphAngle), -Mathf.Cos(glyphAngle))
                        * radius;

                    float rotation = FlipInward
                        ? glyphAngle + Mathf.Pi
                        : glyphAngle;

                    _glyphs.Add(new Glyph
                    {
                        Pos = pos,
                        Rotation = rotation,
                        Angle = glyphAngle,
                        Radius = radius,
                        LineIndex = lineIndex,
                        Ch = ch,
                        Advance = advance
                    });

                    glyphText.Append(ch);

                    minX = Mathf.Min(minX, pos.X);
                    maxX = Mathf.Max(maxX, pos.X);
                    minY = Mathf.Min(minY, pos.Y);
                    maxY = Mathf.Max(maxY, pos.Y);

                    angle += advance / radius;
                }
            }

            _glyphText = glyphText.ToString();

            if (_glyphs.Count == 0)
                return;

            _bboxCenterX = (minX + maxX) / 2f;
            _bboxMinY = minY;
            _bboxHeight = (maxY - minY) + FontSize * 2f;
        }

        private void BuildBlankRuns() {
            _blankRuns = new List<(int, int)>();

            if (_glyphText.Length == 0)
                return;

            foreach (Match match in BlankRegex.Matches(_glyphText))
                _blankRuns.Add((match.Index, match.Length));
        }

        private Vector2 GetArcCenter() {
            Vector2 shift = new Vector2(
                Size.X / 2f - _bboxCenterX,
                -_bboxMinY + FontSize);

            Vector2 pivot = Size / 2f;
            float tilt = Mathf.DegToRad(TiltDegrees);

            return pivot + (shift - pivot).Rotated(tilt);
        }

        private List<string> WrapAll(float outerRadius, float halfWidth) {
            var result = new List<string>();
            string[] paragraphs = Text.Replace("\r\n", "\n").Split('\n');

            int lineIndex = 0;

            foreach (string paragraph in paragraphs) {
                foreach (string line in WrapParagraph(
                    paragraph,
                    outerRadius,
                    halfWidth,
                    lineIndex)) {
                    result.Add(line);
                    lineIndex++;
                }
            }

            return result;
        }

        private List<string> WrapParagraph(
            string paragraph,
            float outerRadius,
            float halfWidth,
            int startIndex) {
            var lines = new List<string>();
            string[] words = paragraph.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0) {
                lines.Add("");
                return lines;
            }

            string current = "";
            int index = startIndex;
            float maxWidth = MaxLineWidth(outerRadius, index, halfWidth);

            foreach (string word in words) {
                foreach (string piece in SplitLongWord(word, maxWidth)) {
                    string test = current.Length == 0
                        ? piece
                        : current + " " + piece;

                    if (current.Length > 0 && MeasureWidth(test) > maxWidth) {
                        lines.Add(current);
                        index++;
                        maxWidth = MaxLineWidth(
                            outerRadius,
                            index,
                            halfWidth);

                        current = piece;
                    }
                    else
                    {
                        current = test;
                    }
                }
            }

            if (current.Length > 0)
                lines.Add(current);

            return lines;
        }

        private float MaxLineWidth(
            float outerRadius,
            int index,
            float halfWidth) {
            float radius = outerRadius - index * LineHeight;

            if (radius <= 0f)
                return 0f;

            float maxSweep = Mathf.DegToRad(
                Mathf.Clamp(MaxSweepDegrees, 1f, 180f));

            float widthLimitedSweep = 2f * Mathf.Asin(
                Mathf.Clamp(halfWidth / radius, 0f, 1f));

            return radius * Mathf.Min(maxSweep, widthLimitedSweep);
        }

        private IEnumerable<string> SplitLongWord(
            string word,
            float maxWidth) {
            if (MeasureWidth(word) <= maxWidth) {
                yield return word;
                yield break;
            }

            string chunk = "";

            foreach (char ch in word) {
                string test = chunk + ch;

                if (chunk.Length > 0 && MeasureWidth(test) > maxWidth) {
                    yield return chunk;
                    chunk = ch.ToString();
                }
                else
                {
                    chunk = test;
                }
            }

            if (chunk.Length > 0)
                yield return chunk;
        }

        private float MeasureWidth(string text) {
            return FontStyle.GetStringSize(
                text,
                HorizontalAlignment.Left,
                -1,
                FontSize).X;
        }

        public override void _Draw() {
            EnsureLayout();

            if (_glyphs.Count == 0)
                return;

            Vector2 shift = new Vector2(
                Size.X / 2f - _bboxCenterX,
                -_bboxMinY + FontSize);

            Vector2 pivot = Size / 2f;
            float tilt = Mathf.DegToRad(TiltDegrees);
            Vector2 tiltDir = new Vector2(
                Mathf.Cos(tilt),
                Mathf.Sin(tilt));
            Vector2 tiltPerp = new Vector2(
                -Mathf.Sin(tilt),
                Mathf.Cos(tilt));

            foreach (Glyph glyph in _glyphs) {
                Vector2 p = glyph.Pos + shift - pivot;
                Vector2 rotated =
                    pivot + tiltDir * p.X + tiltPerp * p.Y;

                DrawSetTransform(
                    rotated,
                    glyph.Rotation + tilt,
                    Vector2.One);

                DrawChar(
                    FontStyle,
                    new Vector2(-glyph.Advance / 2f, 0f),
                    glyph.Ch.ToString(),
                    FontSize,
                    TextColor);
            }

            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }
    }
}

