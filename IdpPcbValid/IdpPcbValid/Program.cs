using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

const string kiCadFn = @"C:\Users\miha\Desktop\vezje-idp\kicad\idp\idp-valid.kicad_pcb";
const string scanFn = @"C:\Users\miha\Desktop\vezje-idp\v4\back-mirrored-300dpi.jpg"; // NOTE: this is just used to get the size
const string maskFn = @"C:\Users\miha\Desktop\vezje-idp\kicad\IdpPcbValid\img\mask-back-300dpi.png";

// side selector
//const string sideSel = "F.Cu";
const string sideSel = "B.Cu";

string pcb = File.ReadAllText(kiCadFn);

// read vias
// format: (via (at 140.97 78.74) (size 0.8) (drill 0.4)
var vias  = new List<Via>();
var m0 = new Regex(@"\(via .*?$", RegexOptions.Multiline).Match(pcb);
var r = new Regex(@"\(via \(at ([^ ]+) ([^)]+)\) \(size ([^)]+)\) \(drill ([^)]+)\)");
while (m0.Success)
{
    var m = r.Match(m0.Value);
    if (!m.Success)
    {
        Console.WriteLine($"No match: {m0.Value}");
    }
    else
    {
        vias.Add(new Via
        {
            x = Convert.ToSingle(m.Result("$1")),
            y = Convert.ToSingle(m.Result("$2")),
            size = Convert.ToSingle(m.Result("$3")),
            drill = Convert.ToSingle(m.Result("$4"))
        });
    }
    m0 = m0.NextMatch();
}

Console.WriteLine($"Loaded vias: {vias.Count}");

// read segments
// format: (segment (start 267.97 223.52) (end 271.78 223.52) (width 0.25) (layer "B.Cu") (net 0) (tstamp f6732fd7-6eb9-4b8f-89e0-1e00981737d1))
var segs = new List<Seg>();
m0 = new Regex(@"\(segment .*?$", RegexOptions.Multiline).Match(pcb);
r = new Regex(@"\(segment \(start ([^ ]+) ([^)]+)\) \(end ([^ ]+) ([^)]+)\) \(width ([^)]+)\) \(layer ""([^""]+)""\)");
while (m0.Success)
{
    var m = r.Match(m0.Value);
    if (!m.Success)
    {
        Console.WriteLine($"No match: {m0.Value}");
    }
    else
    {
        segs.Add(new Seg 
        {
            x1 = Convert.ToSingle(m.Result("$1")),
            y1 = Convert.ToSingle(m.Result("$2")),
            x2 = Convert.ToSingle(m.Result("$3")),
            y2 = Convert.ToSingle(m.Result("$4")),
            w = Convert.ToSingle(m.Result("$5")),
            layer = m.Result("$6")
        });
    }
    m0 = m0.NextMatch();
}

Console.WriteLine($"Loaded segments: {segs.Count}");

// read footprints 
// format:
// (footprint "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" (layer "F.Cu")
// (tstamp 00494083-622f-4255-b319-ffa6098ee9e1)
// (at 181.61 114.3 90)
// ...
// (pad "1" thru_hole circle (at 0 0 180) (size 1.6 1.6) (drill 0.8)
var pads = new List<Pad>();
m0 = new Regex(@"\(footprint .*?^\s*$", RegexOptions.Multiline | RegexOptions.Singleline).Match(pcb);
r = new Regex(@"^\s*\(at ([^ ]+) ([^ )]+) ?([^)]*)\)", RegexOptions.Multiline);
int count = 0;
while (m0.Success)
{
    // get coords and orientation
    var m = r.Match(m0.Value);
    if (!m.Success)
    {
        Console.WriteLine("*** Can't get coords and orientation.");
    }
    else
    {
        float x = Convert.ToSingle(m.Result("$1"));
        float y = Convert.ToSingle(m.Result("$2"));
        float rot = m.Result("$3") == "" ? 0 : Convert.ToSingle(m.Result("$3"));
        // read pads
        var rPad = new Regex(@"\(pad ""([^""]*)"" thru_hole ([^ ]+) \(at ([^ ]+) ([^ )]+) ?([^)]*)\) \(size ([^ ]+) [^)]+\) \(drill ([^)]+)\)");
        m = new Regex(@"\(pad .*?$", RegexOptions.Multiline).Match(m0.Value);
        while (m.Success)
        {
            var mPad = rPad.Match(m.Value);
            if (!mPad.Success)
            {
                Console.WriteLine($"No match: {m.Value}");
            }
            else
            {
                pads.Add(new Pad
                {
                    lbl = mPad.Result("$1"),
                    shape = mPad.Result("$2"),
                    x = Convert.ToSingle(mPad.Result("$3")),
                    y = Convert.ToSingle(mPad.Result("$4")),
                    rot = rot = mPad.Result("$5") == "" ? 0 : Convert.ToSingle(mPad.Result("$5")),
                    size = Convert.ToSingle(mPad.Result("$6")),
                    drill = Convert.ToSingle(mPad.Result("$7"))
                }.Transform(x, y, (int)rot));
            }
            m = m.NextMatch();
        }
    }
    m0 = m0.NextMatch();
    count++;
}

Console.WriteLine($"Observed footprints: {count}");
Console.WriteLine($"Loaded pads: {pads.Count}");

// via stats

Console.WriteLine();
Console.WriteLine("VIA STATS");

var counters = new Dictionary<string, int>();
foreach (var via in vias)
{
    string viaKind = $"{via.size} {via.drill}";
    counters.TryGetValue(viaKind, out int c);
    counters[viaKind] = c + 1;
}
foreach (var item in counters)
{
    Console.WriteLine($"{item.Key} : {item.Value}");
}

// seg stats

Console.WriteLine();
Console.WriteLine("SEGMENT STATS");

counters = new Dictionary<string, int>();
foreach (var seg in segs)
{
    string segKind = $"{seg.w}";
    counters.TryGetValue(segKind, out int c);
    counters[segKind] = c + 1;
}
foreach (var item in counters)
{
    Console.WriteLine($"{item.Key} : {item.Value}");
}

// pad stats

Console.WriteLine();
Console.WriteLine("PAD STATS");

counters = new Dictionary<string, int>();
foreach (var pad in pads)
{
    string padKind = $"{pad.shape} {pad.size} {pad.drill}";
    counters.TryGetValue(padKind, out int c);
    counters[padKind] = c + 1;
}
foreach (var item in counters)
{
    Console.WriteLine($"{item.Key} : {item.Value}");
}

// export the pads (to see if the transformations are OK)

var img = (Bitmap)Image.FromFile(scanFn);
var mask = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
var g = Graphics.FromImage(mask);

foreach (var via in vias)
{
    //via.Render(g);
}
foreach (var pad in pads)
{
    //pad.Render(g);
}
foreach (var seg in segs.Where(x => x.layer == sideSel))
{
    seg.Render(g);
}

mask.Save(maskFn, ImageFormat.Png);

abstract class KiCadObject
{
    protected const int ofsX = -729;
    protected const int ofsY = -640;
    protected const int dpi = 300;

    public abstract void Render(Graphics g);
}

class Seg : KiCadObject
{
    public float x1, y1;
    public float x2, y2;
    public float w;
    public string layer;
    public override void Render(Graphics g)
    {
        var pen = new Pen(Color.Black, w * 0.0393701f * dpi);
        pen.StartCap = pen.EndCap = LineCap.Round;
        g.DrawLine(pen, x1 * 0.0393701f * dpi + ofsX + 9, y1 * 0.0393701f * dpi + ofsY + 9, 
            x2 * 0.0393701f * dpi + ofsX + 9, y2 * 0.0393701f * dpi + ofsY + 9);
    }
}

class Via : KiCadObject
{
    public float x, y;
    public float size;
    public float drill;

    public override void Render(Graphics g)
    {
        g.FillEllipse(Brushes.Black, x * 0.0393701f * dpi + ofsX, y * 0.0393701f * dpi + ofsY, 18, 18);
    }
}

class Pad : KiCadObject
{
    public float x, y;
    public float rot; // rotation 
    public string lbl; // label
    public string shape;
    public float size;
    public float drill;

    public Pad Transform(float x, float y, int rot)
    {
        // rotate
        if (rot < 0) { rot += 360; }
        for (int i = 0; i < rot / 90; i++)
        {
            float tmp = this.y;
            this.y = -this.x;
            this.x = tmp;
        }
        // translate
        this.x += x;
        this.y += y;
        return this;
    }

    public override void Render(Graphics g)
    {
        g.FillEllipse(Brushes.Red, x * 0.0393701f * dpi + ofsX, y * 0.0393701f * dpi + ofsY, 18, 18);
    }
}