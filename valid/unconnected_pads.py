import pcbnew

board = pcbnew.GetBoard()
board.BuildConnectivity()
connectivity = board.GetConnectivity()

no_net = []
has_net = []

for fp in board.GetFootprints():
    ref = fp.GetReference()
    for pad in fp.Pads():
        if len(connectivity.GetConnectedTracks(pad)) == 0:
            if pad.GetNetCode() == 0:
                no_net.append((ref, pad))
            else:
                net_name = pad.GetNet().GetNetname()
                has_net.append(f"{ref}/{pad.GetNumber()} ({net_name})")

print("=== UNCONNECTED, NO NET ===")
for ref, pad in sorted(no_net, key=lambda x: x[0]+x[1].GetNumber()):
    print(f"{ref}/{pad.GetNumber()}")
    net_name = f"unconnected-({ref}-Pad{pad.GetNumber()})"
    net = pcbnew.NETINFO_ITEM(board, net_name)
    board.Add(net)
    pad.SetNet(net)
print(f"Total: {len(no_net)}")

print("\n=== UNCONNECTED, HAS NET ===")
for p in sorted(has_net):
    print(p)
print(f"Total: {len(has_net)}")

board.Save(board.GetFileName())
pcbnew.Refresh()
print("\nSaved.")
