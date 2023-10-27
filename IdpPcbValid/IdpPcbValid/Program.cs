using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Newtonsoft.Json;

static void FloodFill(Bitmap img, IEnumerable<(int x, int y)> startPos, Color color, IEnumerable<KiCadObject> padsAndVias, int netId)
{
    static bool CanFlow(Color cPx, Color cFF, int x, int y) 
    {
        if (cFF == Color.Red && (cPx.B == 255 && cPx.G == 0 && cPx.R == 0))
        {
            Console.Write($"{x},{y} ");
            Console.ReadLine();
        }
        return cPx.A == 255 && (cPx.R != cFF.R || cPx.G != cFF.G || cPx.B != cFF.B); 
    }
    var stack = new Stack<(int x, int y)>();
    startPos.ToList().ForEach(stack.Push);
    while (stack.Count > 0)
    { 
        // get the next "pixel"
        var (x, y) = stack.Pop(); 
        img.SetPixel(x, y, color);
        // check if we hit some object
        float xmm = KiCadObject.XFromPx(x);
        float ymm = KiCadObject.YFromPx(y);
        foreach (var obj in padsAndVias.Where(x => x.CheckHit(xmm, ymm))) { obj.netId = netId; }
        // push the neighboring pixels onto the stack
        if (x + 1 < img.Width && CanFlow(img.GetPixel(x + 1, y), color, x + 1, y)) { stack.Push((x + 1, y)); }
        if (x - 1 >= 0 && CanFlow(img.GetPixel(x - 1, y), color, x - 1, y)) { stack.Push((x - 1, y)); }
        if (y + 1 < img.Height && CanFlow(img.GetPixel(x, y + 1), color, x, y + 1)) { stack.Push((x, y + 1)); }
        if (y - 1 >= 0 && CanFlow(img.GetPixel(x, y - 1), color, x, y - 1)) { stack.Push((x, y - 1)); }
    }
}

static void GraphFill(NetNode node, int netId)
{
    var stack = new Stack<NetNode>();
    stack.Push(node);
    while (stack.Count > 0)
    {
        var n = stack.Pop();
        n.netId = netId;
        // push the connected nodes onto the stack
        foreach (var nn in n.nodes.Where(x => x.netId == -1)) 
        { 
            stack.Push(nn); 
        }
    }
}

static void FindPaths(NetNode node, int startPcbNetId, HashSet<int> pcbNetIds, HashSet<NetNode> tabu, List<NetNode> path, StreamWriter wr) // DFS
{
    tabu.Add(node);
    //wr.WriteLine(node.name);
    if (node.pcbNetId != -1 && node.pcbNetId != startPcbNetId && path.Last(x => x.pcbNetId != -1).pcbNetId != node.pcbNetId)
    {
        wr.WriteLine(path.Select(x => x.name).Aggregate((a, b) => $"{a} -> {b}") + $" -> {node.name}");
    }
    path.Add(node);
    foreach (var n in node.nodes.Where(x => (pcbNetIds.Contains(x.pcbNetId) || x.pcbNetId == -1) && !tabu.Contains(x)))
    {
        FindPaths(n, startPcbNetId, pcbNetIds, tabu, path, wr);
    }
    path.RemoveAt(path.Count - 1);  
}

const string kiCadFn = @"C:\Users\miha\Desktop\vezje-idp\kicad\idp\idp-valid.kicad_pcb";
const string scanPath = @"C:\Users\miha\Desktop\vezje-idp\v4"; 
const string maskPath = @"C:\Users\miha\Desktop\vezje-idp\kicad\IdpPcbValid\img";
const string schFn = @"C:\Users\miha\Desktop\vezje-idp\kicad\sch\sch.txt";
const string cacheFn = @"C:\Users\miha\Desktop\vezje-idp\kicad\sch\pcb_nets_cache.json";

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
            x = Convert.ToSingle(m.Result("$1")),
            y = Convert.ToSingle(m.Result("$2")),
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
// (fp_text reference "R13" 
var rRef = new Regex(@"\(fp_text reference ""([^""]*)""", RegexOptions.Multiline);
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
        Match mRef = rRef.Match(m0.Value);
        string fpRef = mRef.Success ? mRef.Result("$1") : null;
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
                    fpRef = fpRef,
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

// process schema networks

var nodes = new Dictionary<string, NetNode>();
string implCompName = "";

var padRegex = new Regex(@"^[A-Z][A-Z0-9]*/[A-Z0-9]+$");
var netNameStats = new Dictionary<string, int>();

foreach (var line in File.ReadAllLines(schFn).Select(x => x.Trim()).Where(x => !x.StartsWith("--")))
{
    string ln = line.Trim();
    if (ln.StartsWith("<"))
    {
        implCompName = ln.Trim('<', '>');
        continue;
    }
    // resolve pad names
    ln = Regex.Replace(ln, @"(?<=\s|^)(\d+)(?=\s|$)", $"{implCompName}/$1");
    // resolve memory banks
    ln = Regex.Replace(ln, @"BANK1/(\d+)", $"E59/$1 E60/$1 E61/$1 E62/$1 E71/$1 E72/$1 E73/$1 E74/$1");
    ln = Regex.Replace(ln, @"BANK2/(\d+)", $"E85/$1 E86/$1 E87/$1 E88/$1 E98/$1 E99/$1 E100/$1 E101/$1");
    //Console.WriteLine(line);
    //Console.WriteLine("= " + ln);
    var nodeNames = ln.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    foreach (var nn in nodeNames.Where(x => !padRegex.Match(x).Success))
    {
        if (!netNameStats.TryGetValue(nn, out int c)) { c = 0; }
        netNameStats[nn] = c + 1;
    }
    for (int i = 0; i < nodeNames.Length; i++)
    {
        for (int j = i + 1; j < nodeNames.Length; j++)
        {
            if (!nodes.TryGetValue(nodeNames[i], out var node1)) { nodes[nodeNames[i]] = node1 = new NetNode { name = nodeNames[i] }; }
            if (!nodes.TryGetValue(nodeNames[j], out var node2)) { nodes[nodeNames[j]] = node2 = new NetNode { name = nodeNames[j] }; }
            node1.nodes.Add(node2);
            node2.nodes.Add(node1); 
        }
    }
}

// output net name stats

//foreach (var item in netNameStats.OrderByDescending(x => x.Value))
//{
//    Console.WriteLine($"{item.Key} : {item.Value}");
//}

// compute networks in the schema graph

var uncfNodes = nodes.Values.ToList();
int netId = 0;

while (uncfNodes.Count > 0)
{
    GraphFill(uncfNodes.First(), netId);

    uncfNodes = uncfNodes.Where(x => x.netId == -1).ToList();

    netId++;
    Console.Write("*");
}

Console.WriteLine();
Console.WriteLine($"Discovered {netId} nets in the schema network.");

// output networks

//foreach (var net in nodes.Values.GroupBy(x => x.netId))
//{
//    Console.WriteLine($"Net ID: {net.Key}");
//    foreach (var item in net)
//    {
//        Console.WriteLine($"- {item.name}");
//    }
//}

// render flood fill masks

var img = (Bitmap)Image.FromFile(Path.Combine(scanPath, "back-mirrored-300dpi.jpg"));
var ffMaskBack = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
var g = Graphics.FromImage(ffMaskBack);

foreach (var seg in segs.Where(x => x.layer == "B.Cu"))
{
    seg.Render(g, Brushes.Black);
}
foreach (var pad in pads)
{
    pad.Render(g, Brushes.Black);
}
foreach (var via in vias)
{
    var brush = Brushes.Black;
    if (!segs.Any(s => (s.x == via.x && s.y == via.y) || (s.x2 == via.x && s.y2 == via.y))) // vias to which no segment connects directly
    {
        //brush = Brushes.Red;
    }
    via.Render(g, brush);
}

//ffMaskBack.Save(Path.Combine(maskPath, "ff-mask-back.png"), ImageFormat.Png);

img = (Bitmap)Image.FromFile(Path.Combine(scanPath, "front-300dpi.jpg"));
var ffMaskFront = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
g = Graphics.FromImage(ffMaskFront);

foreach (var seg in segs.Where(x => x.layer == "F.Cu"))
{
    seg.Render(g, Brushes.Black);
}
foreach (var pad in pads)
{
    pad.Render(g, Brushes.Black);
}
foreach (var via in vias)
{
    via.Render(g, Brushes.Black);
}

//ffMaskFront.Save(Path.Combine(maskPath, "ff-mask-front.png"), ImageFormat.Png);

// flood fill

var uncfObjs = pads.Select(x => (KiCadObject)x)
    .Concat(vias)
    .ToList();
netId = 0;

// find the short 5V-GND
// find pads connected to ground (1 iteration only)
//var padGnd = pads.First(x => x.fpRef == "C20" && x.lbl == "2");
//var padVcc = pads.First(x => x.fpRef == "R204" && x.lbl == "1");
//FloodFill(ffMaskBack, new[] { (padGnd.XPx, padGnd.YPx) }, Color.Blue, uncfObjs, netId/* 0 = GND */);

//var startObjs = new List<KiCadObject>(new[] { padVcc });
//netId = 1;

//do
//{
//    FloodFill(ffMaskFront, startObjs.Select(x => (x.XPx, x.YPx)), Color.Red, uncfObjs, netId);
//    startObjs = uncfObjs.Where(x => x.netId == netId).ToList();
//    uncfObjs = uncfObjs.Where(x => x.netId <= 0).ToList();
//    FloodFill(ffMaskBack, startObjs.Select(x => (x.XPx, x.YPx)), Color.Red, uncfObjs, netId);
//    startObjs = uncfObjs.Where(x => x.netId == netId).ToList();
//    uncfObjs = uncfObjs.Where(x => x.netId <= 0).ToList();
//    Console.Write(".");
//} while (startObjs.Count > 0);

//ffMaskBack.Save(Path.Combine(maskPath, "ff-mask-back.png"), ImageFormat.Png);
//ffMaskFront.Save(Path.Combine(maskPath, "ff-mask-front.png"), ImageFormat.Png);

//return;

if (File.Exists(cacheFn))
{
    pads = JsonConvert.DeserializeObject<List<Pad>>(File.ReadAllText(cacheFn));
}
else
{
    while (uncfObjs.Count > 0)
    {
        var startObjs = new List<KiCadObject>(new[] { uncfObjs.First() });

        do
        {
            FloodFill(ffMaskFront, startObjs.Select(x => (x.XPx, x.YPx)), Color.Red, uncfObjs, netId);
            startObjs = uncfObjs.Where(x => x.netId == netId).ToList();
            uncfObjs = uncfObjs.Where(x => x.netId == -1).ToList();
            FloodFill(ffMaskBack, startObjs.Select(x => (x.XPx, x.YPx)), Color.Red, uncfObjs, netId);
            startObjs = uncfObjs.Where(x => x.netId == netId).ToList();
            uncfObjs = uncfObjs.Where(x => x.netId == -1).ToList();
            Console.Write(".");
        } while (startObjs.Count > 0);

        netId++;
        Console.Write("*");
    }
}

Console.WriteLine();
Console.WriteLine($"{netId} nets were discovered.");

int snc = pads.GroupBy(x => x.netId).Where(x => x.Count() == 1).Count();
Console.WriteLine($"{snc} nets have one single node.");

// write cache
File.WriteAllText(cacheFn, JsonConvert.SerializeObject(pads));

//ffMaskFront.Save(Path.Combine(maskPath, "ff-mask-front-filled.png"), ImageFormat.Png);

// compare nets

var netsSch = nodes.Values.GroupBy(x => x.netId)
    .Select(x => new {
        netId = x.Key,
        set = x.Where(y => y.name.Contains("/")) // exclude nodes that are not pads
            .Select(y => y.name)
            .ToHashSet(),
        items = x.Where(y => y.name.Contains("/"))
            .ToDictionary(y => y.name, y => y)
    }) 
    .Where(x => x.set.Any())
    .ToList();
var netsPcb = pads.GroupBy(x => x.netId)
    .Select(x => new {
        netId = x.Key,
        set = x.Where(y => !new[] { "C1", "C2", "C3", "C4", "CX" }.Contains(y.fpRef)) // excl. electrolytic and CX caps 
            .Select(y => $"{y.fpRef}/{y.lbl}")
            .ToHashSet(),
        items = x.Where(y => !new[] { "C1", "C2", "C3", "C4", "CX" }.Contains(y.fpRef))
            .ToDictionary(y => $"{y.fpRef}/{y.lbl}", y => y)
    })
    .Where(x => x.set.Count() > 1) // excl. nets with one single node
    .ToList();

Console.WriteLine($"{netsSch.Count} : {netsPcb.Count}");

foreach (var item in nodes.Values)
{
    item.pcbNetId = pads.FirstOrDefault(p => $"{p.fpRef}/{p.lbl}" == item.name)?.netId ?? -1;
}

foreach (var item in pads)
{
    item.schNetId = nodes.Values.FirstOrDefault(n => n.name == $"{item.fpRef}/{item.lbl}")?.netId ?? -1;
}

using (var wr = new StreamWriter(@"C:\Users\miha\Desktop\vezje-idp\kicad\sch\pcb_nets.txt"))
{
    foreach (var net in netsPcb)
    {
        bool skip = false;
        foreach (var net2 in netsSch)
        {
            if (net.set.SetEquals(net2.set))
            {
                // skip nets that match completely
                skip = true;
                break;
            }
        }
        if (skip) { continue; }
        wr.WriteLine();
        wr.WriteLine($"*** NET ID: {net.netId}");
        var hs = new HashSet<int>();
        foreach (var item in net.items.Values)
        {
            if (item.schNetId != -1) { hs.Add(item.schNetId); }
            wr.WriteLine($"{item.fpRef}/{item.lbl} : {item.schNetId}");
        }
        wr.WriteLine($"({hs.Count})");
    }
}

int eqCount = 0;

using (var wr = new StreamWriter(@"C:\Users\miha\Desktop\vezje-idp\kicad\sch\sch_nets.txt"))
{
    foreach (var net in netsSch)
    {
        bool skip = false;
        foreach (var net2 in netsPcb)
        {
            if (net.set.SetEquals(net2.set))
            {
                // skip nets that match completely
                skip = true;
                eqCount++;
                break;
            }
        }
        if (skip) { continue; }
        wr.WriteLine();
        wr.WriteLine($"*** NET ID: {net.netId}");
        var hs = new HashSet<int>();
        foreach (var item in net.items.Values)
        {
            if (item.pcbNetId != -1) { hs.Add(item.pcbNetId); }
            wr.WriteLine($"{item.name} : {item.pcbNetId}");          
        }
        wr.WriteLine($"({hs.Count})");
        var pcbNetIds = net.items.Values.Select(x => x.pcbNetId).ToHashSet();
        foreach (var group in net.items.Values.Where(x => x.pcbNetId != -1).GroupBy(x => x.pcbNetId))
        {
            FindPaths(group.First(), group.First().pcbNetId, pcbNetIds, new HashSet<NetNode>(), new List<NetNode>(), wr);
        }
    }
}

Console.WriteLine($"Discovered {eqCount} equal nets.");

// interactive mode

while (true)
{
    Console.Write("Enter pad identifier: ");
    string padId = Console.ReadLine();
    string fpRef = padId.Split('/')[0].ToUpper();
    string padLbl = padId.Contains('/') ? padId.Split('/')[1] : null;
    if (padLbl == null)
    {
        var fpPads = pads.Where(x => x.fpRef == fpRef);
        if (fpPads.Any())
        {
            foreach (var p in fpPads)
            {
                //Console.Write($"{fpRef}/{p.lbl} ");
                Console.Write($"{p.lbl} ");
                var otherPad = pads.FirstOrDefault(x => x.netId == p.netId && x.schNetId != -1 && x.fpRef != fpRef);
                Console.WriteLine(otherPad != null ? $"{otherPad.fpRef}/{otherPad.lbl}" : "NOT FOUND");
            }
        }
        else 
        {
            Console.WriteLine("Component not found.");
        }
    }
    else
    {
        var pad = pads.FirstOrDefault(x => x.fpRef == fpRef && x.lbl == padLbl);
        if (pad != null)
        {
            Console.WriteLine($"Net ID: {pad.netId}");
            Console.WriteLine("Pads in this net:");
            foreach (var p in pads.Where(x => x.netId == pad.netId))
            {
                Console.WriteLine($"* {p.fpRef}/{p.lbl}");
            }
        }
        else
        {
            Console.WriteLine("Pad not found.");
        }
    }
}

// classes

abstract class KiCadObject
{
    protected const int ofsX = -722;
    protected const int ofsY = -633;
    protected const int dpi = 300;

    public float x, y;
    public float size;
    public int netId = -1;
    public int schNetId = -1;

    public abstract void Render(Graphics g, Brush brush);

    public virtual bool CheckHit(float x, float y)
    {
        float r = size / 2f;
        return x >= this.x - r &&
            y >= this.y - r &&
            x <= this.x + r &&
            y <= this.y + r &&
            Math.Pow(x - this.x, 2) + Math.Pow(y - this.y, 2) <= Math.Pow(r, 2);
    }

    public int XPx => (int)Math.Round(x * 0.0393701f * dpi + ofsX);
    public int YPx => (int)Math.Round(y * 0.0393701f * dpi + ofsY);
    public static float XFromPx(int xPx) => (xPx - ofsX) / (0.0393701f * dpi);
    public static float YFromPx(int yPx) => (yPx - ofsY) / (0.0393701f * dpi);
}

class Seg : KiCadObject
{
    public float x2, y2;
    public float w;
    public string layer;

    public override void Render(Graphics g, Brush brush)
    {
        var color = (brush as SolidBrush)?.Color ?? Color.Black;
        var pen = new Pen(color, w * 0.0393701f * dpi);
        pen.StartCap = pen.EndCap = LineCap.Round;
        g.DrawLine(pen, x * 0.0393701f * dpi + ofsX, y * 0.0393701f * dpi + ofsY, 
            x2 * 0.0393701f * dpi + ofsX, y2 * 0.0393701f * dpi + ofsY);
    }
}

class Via : KiCadObject
{
    public float drill;

    public override void Render(Graphics g, Brush brush)
    {
        float sz = size * 0.0393701f * dpi;
        g.FillEllipse(brush, x * 0.0393701f * dpi - sz/2f + ofsX, y * 0.0393701f * dpi + ofsY - sz/2f, 
            sz, sz);
    }
}

class Pad : KiCadObject
{
    public string fpRef; // footprint ref
    public float rot; // rotation 
    public string lbl; // label
    public string shape;
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

    public override void Render(Graphics g, Brush brush)
    {
        float sz = size * 0.0393701f * dpi;
        g.FillEllipse(brush, x * 0.0393701f * dpi + ofsX - sz/2f, y * 0.0393701f * dpi + ofsY - sz/2f, sz, sz);
    }
}

class NetNode
{
    public string name;
    public List<NetNode> nodes = new();
    public int netId = -1;
    public int pcbNetId = -1;
}