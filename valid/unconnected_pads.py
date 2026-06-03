import pcbnew

board = pcbnew.GetBoard()
board.BuildConnectivity()
connectivity = board.GetConnectivity()


def get_connected_nets(pad):
    nets = set()
    for track in connectivity.GetConnectedTracks(pad):
        n = track.GetNetCode()
        if n != 0:
            nets.add(n)
    return nets

net_cache = {}

def make_unconnected_net(name):
    if name not in net_cache:
        net = pcbnew.NETINFO_ITEM(board, name)
        board.Add(net)
        net_cache[name] = net
    return net_cache[name]

unconnected_no_net = 0
unconnected_has_net = 0
connected_assigned = 0
connected_conflict = 0

for fp in board.GetFootprints():
    ref = fp.GetReference()
    for pad in fp.Pads():
        connected_nets = get_connected_nets(pad)
        physically_connected = len(connectivity.GetConnectedTracks(pad)) > 0

        if not physically_connected:
            if pad.GetNetCode() == 0:
                pad.SetNet(make_unconnected_net(f"unconnected-({ref}-Pad{pad.GetNumber()})"))
                unconnected_no_net += 1
            else:
                net_name = pad.GetNet().GetNetname()
                print(f"WARNING: {ref}/{pad.GetNumber()} unconnected but has net '{net_name}' — renaming")
                pad.SetNet(make_unconnected_net(f"unconnected-{net_name}"))
                unconnected_has_net += 1
        else:
            if pad.GetNetCode() == 0:
                named_nets = {n for n in connected_nets if n != 0}
                if len(named_nets) == 1:
                    net_code = next(iter(named_nets))
                    pad.SetNet(board.FindNet(net_code))
                    connected_assigned += 1
                elif len(named_nets) > 1:
                    net_names = [board.FindNet(n).GetNetname() for n in named_nets]
                    print(f"WARNING: {ref}/{pad.GetNumber()} connected to multiple nets: {', '.join(net_names)}")
                    connected_conflict += 1

print(f"\nUnconnected, no net -> assigned unconnected: {unconnected_no_net}")
print(f"Unconnected, had net -> warned + renamed: {unconnected_has_net}")
print(f"Connected, no net -> net inferred: {connected_assigned}")
print(f"Connected, no net -> conflict (multiple nets): {connected_conflict}")

board.Save(board.GetFileName())
pcbnew.Refresh()
print("Saved.")
