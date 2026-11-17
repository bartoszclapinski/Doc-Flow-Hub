using System.IO.Compression;
using System.Text;

namespace DocFlowHub.Infrastructure.Data.Demo;

/// <summary>What a generated demo picture shows (Sprint 24).</summary>
public enum DemoPicture
{
    BarChart,
    LineChart,
    Wireframe,
    Diagram,
    Donut,
}

/// <summary>
/// Draws the demo's picture documents as PNG files (Sprint 24): charts, a page wireframe, a box-and-arrow
/// diagram and a donut chart. No package: a small RGB canvas and a PNG writer (zlib from the BCL, CRC-32
/// here). The same seed and version always give the same bytes, so a missing file can be rebuilt.
/// </summary>
public static class DemoImageFactory
{
    public const int Width = 960;
    public const int Height = 600;

    private static readonly (byte R, byte G, byte B)[] Palette =
    {
        (47, 107, 222), (5, 150, 105), (217, 119, 6), (124, 58, 237), (219, 39, 119), (8, 145, 178), (220, 38, 38),
    };

    private static readonly (byte, byte, byte) Paper = (250, 250, 251);
    private static readonly (byte, byte, byte) Ink = (55, 65, 81);
    private static readonly (byte, byte, byte) Grid = (226, 229, 234);
    private static readonly (byte, byte, byte) Box = (209, 213, 219);
    private static readonly (byte, byte, byte) BoxDark = (156, 163, 175);

    /// <summary>The PNG of one version of a picture document; <paramref name="seed"/> varies the data between documents.</summary>
    public static byte[] Create(DemoPicture picture, int seed, int version)
    {
        var canvas = new Canvas(Width, Height, Paper);
        var rng = new Random(seed * 31 + 7);
        switch (picture)
        {
            case DemoPicture.BarChart: BarChart(canvas, rng, version); break;
            case DemoPicture.LineChart: LineChart(canvas, rng, version); break;
            case DemoPicture.Wireframe: Wireframe(canvas, rng, version); break;
            case DemoPicture.Diagram: Diagram(canvas, rng, version); break;
            default: Donut(canvas, rng, version); break;
        }
        return canvas.ToPng();
    }

    // ------------------------------------------------------------------ pictures

    private static void Axes(Canvas c, int left, int top, int right, int bottom, int lines)
    {
        for (var i = 0; i <= lines; i++)
        {
            var y = bottom - (bottom - top) * i / lines;
            c.Rect(left, y, right - left, 1, Grid);
        }
        c.Rect(left, top, 2, bottom - top, Ink);
        c.Rect(left, bottom, right - left, 2, Ink);
    }

    private static void BarChart(Canvas c, Random rng, int version)
    {
        const int left = 90, top = 70, right = 900, bottom = 520;
        Axes(c, left, top, right, bottom, 5);
        var bars = 6 + rng.Next(0, 5);
        var series = 1 + rng.Next(0, 2);
        var slot = (right - left - 20) / bars;
        for (var b = 0; b < bars; b++)
        {
            for (var s = 0; s < series; s++)
            {
                // Each version moves the figures a little, so versions differ visibly.
                var value = 0.25 + rng.NextDouble() * 0.6 + (version - 1) * 0.03 * ((b + s) % 3 - 1);
                value = Math.Clamp(value, 0.08, 0.98);
                var h = (int)((bottom - top) * value);
                var w = (slot - 18) / series;
                c.Rect(left + 14 + b * slot + s * w, bottom - h, w - 4, h, Palette[s]);
            }
        }
        Legend(c, series, rng);
    }

    private static void LineChart(Canvas c, Random rng, int version)
    {
        const int left = 90, top = 70, right = 900, bottom = 520;
        Axes(c, left, top, right, bottom, 5);
        var lines = 1 + rng.Next(0, 3);
        const int points = 12;
        for (var l = 0; l < lines; l++)
        {
            var value = 0.3 + rng.NextDouble() * 0.3;
            var trend = (rng.NextDouble() - 0.3) * 0.05 + (version - 1) * 0.004;
            var prev = (X: left, Y: 0);
            for (var p = 0; p < points; p++)
            {
                value = Math.Clamp(value + trend + (rng.NextDouble() - 0.5) * 0.08, 0.05, 0.95);
                var x = left + 10 + (right - left - 20) * p / (points - 1);
                var y = bottom - (int)((bottom - top) * value);
                if (p > 0) c.Line(prev.X, prev.Y, x, y, 4, Palette[l]);
                c.Disc(x, y, 6, Palette[l]);
                prev = (x, y);
            }
        }
        Legend(c, lines, rng);
    }

    private static void Legend(Canvas c, int count, Random rng)
    {
        for (var i = 0; i < count; i++)
        {
            c.Rect(90 + i * 140, 30, 18, 18, Palette[i]);
            c.Rect(116 + i * 140, 36, 70 + rng.Next(0, 30), 6, Box);
        }
    }

    private static void Wireframe(Canvas c, Random rng, int version)
    {
        // Browser frame and header.
        c.Rect(40, 30, 880, 540, (255, 255, 255));
        c.Frame(40, 30, 880, 540, 2, BoxDark);
        c.Rect(40, 30, 880, 34, Box);
        for (var i = 0; i < 3; i++) c.Disc(62 + i * 20, 47, 6, BoxDark);
        c.Rect(70, 84, 120, 26, Palette[0]);
        for (var i = 0; i < 4; i++) c.Rect(520 + i * 95, 92, 70, 10, BoxDark);

        // Hero: version 2 adds a call-to-action button, version 3 splits the hero.
        c.Rect(70, 130, version >= 3 ? 470 : 820, 170, (238, 241, 246));
        c.Rect(100, 170, 300, 18, BoxDark);
        c.Rect(100, 200, 240, 10, Box);
        c.Rect(100, 218, 260, 10, Box);
        if (version >= 2) c.Rect(100, 250, 130, 32, Palette[1]);
        if (version >= 3)
        {
            c.Rect(560, 130, 330, 170, (230, 236, 248));
            c.Line(560, 130, 890, 300, 2, Box);
            c.Line(890, 130, 560, 300, 2, Box);
        }

        // Cards: more of them in later versions.
        var cards = Math.Min(3 + (version - 1), 6);
        var width = (820 - (cards - 1) * 20) / cards;
        for (var i = 0; i < cards; i++)
        {
            var x = 70 + i * (width + 20);
            c.Frame(x, 330, width, 200, 2, Box);
            c.Rect(x + 16, 346, width - 32, 80, (238, 241, 246));
            c.Rect(x + 16, 442, width - 60, 12, BoxDark);
            c.Rect(x + 16, 464, width - 40, 8, Box);
            c.Rect(x + 16, 480, width - 70 - rng.Next(0, 30), 8, Box);
        }
    }

    private static void Diagram(Canvas c, Random rng, int version)
    {
        // Boxes in two or three layers, joined by lines; later versions add a box (a new service).
        var layers = new[] { 2, 3, 2 + Math.Min(version - 1, 2) };
        var centres = new List<(int X, int Y)>[layers.Length];
        for (var l = 0; l < layers.Length; l++)
        {
            centres[l] = new();
            var y = 110 + l * 190;
            for (var i = 0; i < layers[l]; i++)
            {
                var x = 480 + (i - (layers[l] - 1) / 2.0) * 230;
                centres[l].Add(((int)x, y));
            }
        }
        for (var l = 1; l < layers.Length; l++)
        {
            foreach (var to in centres[l])
            {
                // The nearest box above, so the lines do not cross.
                var from = centres[l - 1].OrderBy(c => Math.Abs(c.X - to.X)).First();
                c.Line(from.X, from.Y + 40, to.X, to.Y - 40, 3, BoxDark);
                c.Disc(to.X, to.Y - 42, 6, BoxDark);
            }
        }
        for (var l = 0; l < layers.Length; l++)
        {
            foreach (var (x, y) in centres[l])
            {
                var colour = Palette[(l * 2 + x / 230) % Palette.Length];
                c.Rect(x - 90, y - 40, 180, 80, (255, 255, 255));
                c.Frame(x - 90, y - 40, 180, 80, 3, colour);
                c.Rect(x - 60, y - 6, 120, 12, Box);
                // Version 4 renames the gateway (the first box): a second, darker label line.
                if (version >= 4 && l == 0 && (x, y) == centres[0][0]) c.Rect(x - 60, y + 12, 80, 8, BoxDark);
            }
        }
    }

    private static void Donut(Canvas c, Random rng, int version)
    {
        var parts = 3 + rng.Next(0, 3);
        var weights = Enumerable.Range(0, parts).Select(i => 1 + rng.NextDouble() * 3 + (i == 0 ? (version - 1) * 0.3 : 0)).ToArray();
        var total = weights.Sum();
        const int cx = 330, cy = 300, outer = 220, inner = 120;
        for (var y = cy - outer; y <= cy + outer; y++)
        {
            for (var x = cx - outer; x <= cx + outer; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var d2 = dx * dx + dy * dy;
                if (d2 > outer * outer || d2 < inner * inner) continue;
                var angle = (Math.Atan2(dy, dx) + Math.PI * 2.5) % (Math.PI * 2) / (Math.PI * 2);
                double sum = 0;
                var part = 0;
                for (; part < parts - 1; part++)
                {
                    sum += weights[part] / total;
                    if (angle < sum) break;
                }
                c.Set(x, y, Palette[part]);
            }
        }
        for (var i = 0; i < parts; i++)
        {
            c.Rect(640, 160 + i * 56, 24, 24, Palette[i]);
            c.Rect(678, 166 + i * 56, 120 + (int)(weights[i] * 20), 10, Box);
        }
    }

    // ------------------------------------------------------------------ canvas + PNG

    private sealed class Canvas
    {
        private readonly int _w;
        private readonly int _h;
        private readonly byte[] _rgb;

        public Canvas(int w, int h, (byte R, byte G, byte B) background)
        {
            _w = w;
            _h = h;
            _rgb = new byte[w * h * 3];
            for (var i = 0; i < _rgb.Length; i += 3)
            {
                _rgb[i] = background.R;
                _rgb[i + 1] = background.G;
                _rgb[i + 2] = background.B;
            }
        }

        public void Set(int x, int y, (byte R, byte G, byte B) c)
        {
            if (x < 0 || y < 0 || x >= _w || y >= _h) return;
            var i = (y * _w + x) * 3;
            _rgb[i] = c.R;
            _rgb[i + 1] = c.G;
            _rgb[i + 2] = c.B;
        }

        public void Rect(int x, int y, int w, int h, (byte, byte, byte) c)
        {
            for (var yy = Math.Max(0, y); yy < Math.Min(_h, y + h); yy++)
            for (var xx = Math.Max(0, x); xx < Math.Min(_w, x + w); xx++)
                Set(xx, yy, c);
        }

        public void Frame(int x, int y, int w, int h, int t, (byte, byte, byte) c)
        {
            Rect(x, y, w, t, c);
            Rect(x, y + h - t, w, t, c);
            Rect(x, y, t, h, c);
            Rect(x + w - t, y, t, h, c);
        }

        public void Disc(int cx, int cy, int r, (byte, byte, byte) c)
        {
            for (var y = -r; y <= r; y++)
            for (var x = -r; x <= r; x++)
                if (x * x + y * y <= r * r) Set(cx + x, cy + y, c);
        }

        public void Line(int x0, int y0, int x1, int y1, int thickness, (byte, byte, byte) c)
        {
            var steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            var r = Math.Max(0, thickness / 2);
            for (var i = 0; i <= steps; i++)
            {
                var x = x0 + (x1 - x0) * i / Math.Max(1, steps);
                var y = y0 + (y1 - y0) * i / Math.Max(1, steps);
                if (r == 0) Set(x, y, c); else Rect(x - r, y - r, thickness, thickness, c);
            }
        }

        public byte[] ToPng()
        {
            // Raw scanlines, each starting with filter type 0.
            var raw = new byte[_h * (_w * 3 + 1)];
            for (var y = 0; y < _h; y++)
            {
                Buffer.BlockCopy(_rgb, y * _w * 3, raw, y * (_w * 3 + 1) + 1, _w * 3);
            }

            using var png = new MemoryStream();
            png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            var header = new byte[13];
            WriteBigEndian(header, 0, _w);
            WriteBigEndian(header, 4, _h);
            header[8] = 8;   // bit depth
            header[9] = 2;   // colour type: RGB
            Chunk(png, "IHDR", header);

            using (var compressed = new MemoryStream())
            {
                using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(raw);
                }
                Chunk(png, "IDAT", compressed.ToArray());
            }
            Chunk(png, "IEND", Array.Empty<byte>());
            return png.ToArray();
        }

        private static void Chunk(Stream png, string type, byte[] data)
        {
            var length = new byte[4];
            WriteBigEndian(length, 0, data.Length);
            png.Write(length);
            var typeBytes = Encoding.ASCII.GetBytes(type);
            png.Write(typeBytes);
            png.Write(data);
            var crc = Crc32(typeBytes, data);
            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, (int)crc);
            png.Write(crcBytes);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
        {
            var c = (uint)n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            return c;
        }).ToArray();

        private static uint Crc32(byte[] type, byte[] data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in type) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }
    }
}
