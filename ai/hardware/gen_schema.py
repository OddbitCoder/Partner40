import re
from collections import defaultdict

PCB_FILE = "../../pcb/idp.kicad_pcb"
COMPONENTS_FILE = "components.md"
OUTPUT_FILE = "schema.md"

# load value refs from components.md
value_ref = {}
with open(COMPONENTS_FILE, "r", encoding="utf-8") as f:
    for line in f:
        m = re.match(r'\|\s*([\w\d\-–]+)\s*\|\s*([^|]+?)\s*\|', line)
        if m:
            comp_id = m.group(1).strip()
            val = m.group(2).strip()
            if comp_id and comp_id != "ID" and not comp_id.startswith("-"):
                value_ref[comp_id] = val

# parse PCB file
with open(PCB_FILE, "r", encoding="utf-8") as f:
    lines = f.readlines()

# net id -> net name
net_names = {}
for line in lines:
    m = re.match(r'\s*\(net (\d+) "([^"]*)"\)', line)
    if m:
        net_names[int(m.group(1))] = m.group(2)

# parse footprints: ref -> {pad_num -> net_name}
footprints = {}   # ref -> {pad -> net_name}
fp_order = []     # preserve order

current_ref = None
current_pads = {}

i = 0
while i < len(lines):
    line = lines[i]

    if re.match(r'\t\(footprint ', line):
        if current_ref:
            footprints[current_ref] = current_pads
            fp_order.append(current_ref)
        current_ref = None
        current_pads = {}

    m = re.search(r'\(property "Reference" "([^"]+)"', line)
    if m:
        current_ref = m.group(1)

    m = re.match(r'\s+\(pad "([^"]+)"', line)
    if m and current_ref:
        pad_num = m.group(1)
        # look ahead for net in this pad block
        depth = line.count('(') - line.count(')')
        j = i + 1
        net_name = None
        while j < len(lines) and depth > 0:
            depth += lines[j].count('(') - lines[j].count(')')
            nm = re.search(r'\(net \d+ "([^"]*)"\)', lines[j])
            if nm:
                net_name = nm.group(1)
            j += 1
        current_pads[pad_num] = net_name or ""

    i += 1

if current_ref:
    footprints[current_ref] = current_pads
    fp_order.append(current_ref)

# deduplicate order
seen = set()
fp_order = [r for r in fp_order if not (r in seen or seen.add(r))]

# net -> list of (ref, pad)
net_pads = defaultdict(list)
for ref, pads in footprints.items():
    for pad, net in pads.items():
        if net:
            net_pads[net].append((ref, pad))

POWER_NETS = {"GND", "5V", "12V", "-12V"}

def sort_key(ref):
    m = re.match(r'([A-Za-z]+)(\d+)', ref)
    return (m.group(1), int(m.group(2))) if m else (ref, 0)

# write schema.md
with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
    f.write("# Partner 40 Schema\n\n")
    f.write("For each component and pin, lists all connected component/pins (same net).\n")
    f.write("Power nets (GND, 5V, 12V, -12V) are listed in the Power Nets chapter; ")
    f.write("component entries show only the net name for those pins.\n\n")

    # Power Nets chapter
    f.write("## Power Nets\n\n")
    for net in sorted(POWER_NETS):
        pads = net_pads.get(net, [])
        if not pads:
            continue
        pads_sorted = sorted(pads, key=lambda x: (sort_key(x[0]), x[1]))
        connections = ", ".join(f"{r}/{p}" for r, p in pads_sorted)
        f.write(f"**{net}**: {connections}\n\n")

    def is_decoupling_cx(ref, pads):
        return re.match(r'CX\d+$', ref) and all(n in POWER_NETS or not n for n in pads.values())

    # collect CX decoupling caps
    cx_caps = [r for r in sorted(fp_order, key=sort_key) if is_decoupling_cx(r, footprints[r])]
    cx_set = set(cx_caps)

    # CX group
    if cx_caps:
        f.write("## Decoupling capacitors\n\n")
        f.write(f"{', '.join(cx_caps)} — all connect to 5V and GND.\n\n")

    # Component chapters
    for ref in sorted(fp_order, key=sort_key):
        if ref in cx_set:
            continue
        pads = footprints[ref]
        f.write(f"## {ref}\n\n")
        f.write("| Pin | Net | Connected to |\n")
        f.write("|--|--|--|\n")

        for pad in sorted(pads, key=lambda x: int(x) if x.isdigit() else x):
            net = pads[pad]
            if not net:
                connections = ""
            elif net in POWER_NETS:
                connections = "*(see Power Nets)*"
            else:
                others = [(r, p) for r, p in net_pads[net] if not (r == ref and p == pad)]
                others_sorted = sorted(others, key=lambda x: (sort_key(x[0]), x[1]))
                connections = ", ".join(f"{r}/{p}" for r, p in others_sorted)
            f.write(f"| {pad} | {net} | {connections} |\n")

        f.write("\n")

print(f"Written {len(fp_order)} components to {OUTPUT_FILE}")
