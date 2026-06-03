import pcbnew
from collections import defaultdict

board = pcbnew.GetBoard()
tracks = list(board.GetTracks())
pads = [p for fp in board.GetFootprints() for p in fp.Pads()]

print(f"Tracks/vias: {len(tracks)}, Pads: {len(pads)}")
print(f"Unassigned tracks/vias: {sum(1 for t in tracks if t.GetNetCode() == 0)}")

# union-find
parent = {}
def find(x):
    while parent.get(x, x) != x:
        parent[x] = parent.get(parent[x], parent[x])
        x = parent[x]
    return x
def union(a, b):
    a, b = find(a), find(b)
    if a != b:
        parent[b] = a

layer_pos_map = defaultdict(list)
VIA_ALL = -1

for pad in pads:
    p = pad.GetPosition()
    for layer in pad.GetLayerSet().Seq():
        layer_pos_map[((p.x, p.y), layer)].append(pad)

for track in tracks:
    if track.GetClass() == 'PCB_VIA':
        p = track.GetPosition()
        layer_pos_map[((p.x, p.y), VIA_ALL)].append(track)
    else:
        layer = track.GetLayer()
        s, e = track.GetStart(), track.GetEnd()
        layer_pos_map[((s.x, s.y), layer)].append(track)
        layer_pos_map[((e.x, e.y), layer)].append(track)

via_positions = set()
for track in tracks:
    if track.GetClass() == 'PCB_VIA':
        p = track.GetPosition()
        via_positions.add((p.x, p.y))

for key, items in layer_pos_map.items():
    for i in range(1, len(items)):
        union(id(items[0]), id(items[i]))

for pos in via_positions:
    via_items = layer_pos_map.get((pos, VIA_ALL), [])
    if not via_items:
        continue
    via_id = id(via_items[0])
    for layer_key, items in layer_pos_map.items():
        if layer_key[0] == pos:
            for item in items:
                union(via_id, id(item))

group_net = {}
for pad in pads:
    if pad.GetNetCode() != 0:
        g = find(id(pad))
        if g not in group_net:
            group_net[g] = pad.GetNet()

for track in tracks:
    if track.GetNetCode() != 0:
        g = find(id(track))
        if g not in group_net:
            group_net[g] = track.GetNet()

assigned = 0
for track in tracks:
    if track.GetNetCode() != 0:
        continue
    g = find(id(track))
    if g in group_net:
        track.SetNet(group_net[g])
        assigned += 1

print(f"Assigned: {assigned}")
print(f"Still unassigned: {sum(1 for t in tracks if t.GetNetCode() == 0)}")

pcbnew.Refresh()
board.Save(board.GetFileName())
print("Saved.")
