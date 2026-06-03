from collections import defaultdict
from datetime import datetime

SWAP_PINS = {"CX10", "CX16", "CX17", "CX20", "CX27", "CX28",
             "CX35", "CX2", "CX13", "CX11", "CX25",
             "CX24", "CX33", "CX26", "CX29", "CX3"}

def maybe_swap(comp, pin):
    if comp in SWAP_PINS:
        return "2" if pin == "1" else "1" if pin == "2" else pin
    return pin

nets = defaultdict(list)  # net_name -> [(comp, pin), ...]

with open("nets.txt", "r") as f:
    current_net = None
    for line in f:
        line = line.rstrip()
        if line.startswith("NET "):
            current_net = line[4:]
        elif line.startswith("  ") and current_net:
            comp, pin = line.strip().split("/", 1)
            nets[current_net].append((comp, maybe_swap(comp, pin)))

components = set()
for pins in nets.values():
    for comp, _ in pins:
        components.add(comp)

out = []
out.append('(export (version "E")')
out.append('  (design')
out.append(f'    (source "partner40")')
out.append(f'    (date "{datetime.now().isoformat()}")')
out.append('    (tool "manual")')
out.append('    (sheet (number "1") (name "/") (tstamps "/")))')

out.append('  (components')
for ref in sorted(components):
    out.append(f'    (comp (ref "{ref}")')
    out.append(f'      (value "")')
    out.append(f'      (footprint "")')
    out.append(f'      (datasheet ""))')
out.append('  )')

out.append('  (nets')
for code, (net_name, pins) in enumerate(sorted(nets.items()), start=1):
    out.append(f'    (net (code "{code}") (name "{net_name}") (class "Default")')
    for comp, pin in sorted(pins):
        out.append(f'      (node (ref "{comp}") (pin "{pin}") (pintype "passive"))')
    out.append('    )')
out.append('  )')
out.append(')')

with open("idp.net", "w") as f:
    f.write('\n'.join(out) + '\n')

print(f"Written {len(nets)} nets, {len(components)} components to idp.net")
