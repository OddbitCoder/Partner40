from collections import defaultdict

nets = defaultdict(set)   # canonical_name -> set of (component, pin)
pin_to_net = {}           # (component, pin) -> canonical_name
aliases = {}              # any_name -> canonical_name

def canonical(name):
    while name in aliases:
        name = aliases[name]
    return name

def merge_names(a, b):
    """Make b an alias of a (a is canonical)."""
    ca, cb = canonical(a), canonical(b)
    if ca == cb:
        return ca
    # prefer named over anonymous
    if cb.startswith("_NET_") or (not ca.startswith("_NET_") and ca <= cb):
        aliases[cb] = ca
        nets[ca] |= nets.pop(cb, set())
        for k in list(pin_to_net):
            if pin_to_net[k] == cb:
                pin_to_net[k] = ca
        return ca
    else:
        aliases[ca] = cb
        nets[cb] |= nets.pop(ca, set())
        for k in list(pin_to_net):
            if pin_to_net[k] == ca:
                pin_to_net[k] = cb
        return cb

def assign(net_name, comp, pin):
    cn = canonical(net_name)
    key = (comp, str(pin))
    if key in pin_to_net:
        existing = pin_to_net[key]
        if existing != cn:
            cn = merge_names(existing, cn)
    nets[cn].add(key)
    pin_to_net[key] = cn

anon_counter = [0]
def new_anon():
    anon_counter[0] += 1
    return f"_NET_{anon_counter[0]}"

BANKS = {
    "BANK1": ["E59", "E60", "E61", "E62", "E71", "E72", "E73", "E74"],
    "BANK2": ["E85", "E86", "E87", "E88", "E98", "E99", "E100", "E101"],
}

with open("sch.txt", "r") as f:
    lines = f.readlines()

context = None

for line in lines:
    line = line.strip()
    if not line or line.startswith("--"):
        continue
    if line.startswith("<") and line.endswith(">"):
        context = line[1:-1].strip() or None
        continue

    tokens = line.split()
    components = []
    net_names = []

    for token in tokens:
        if "/" in token:
            comp, pin = token.split("/", 1)
            if comp in BANKS:
                for expanded in BANKS[comp]:
                    components.append((expanded, pin))
            else:
                components.append((comp, pin))
        elif token.isdigit() and context:
            if context in BANKS:
                for expanded in BANKS[context]:
                    components.append((expanded, token))
            else:
                components.append((context, token))
        else:
            net_names.append(token)

    if not components and not net_names:
        continue

    if net_names:
        net_name = canonical(net_names[0])
        for extra in net_names[1:]:
            net_name = merge_names(net_name, extra)
    else:
        net_name = None
        for comp, pin in components:
            existing = pin_to_net.get((comp, str(pin)))
            if existing:
                net_name = existing
                break
        if not net_name:
            net_name = new_anon()

    for comp, pin in components:
        assign(net_name, comp, pin)

with open("nets.txt", "w") as f:
    for net_name in sorted(nets):
        pins = sorted(nets[net_name])
        if not pins:
            continue
        f.write(f"NET {net_name}\n")
        for comp, pin in pins:
            f.write(f"  {comp}/{pin}\n")

anon_count = sum(1 for n in nets if n.startswith("_NET_"))
print(f"Total nets: {len(nets)}")
print(f"Anonymous nets: {anon_count}")
print(f"Total pins assigned: {len(pin_to_net)}")
