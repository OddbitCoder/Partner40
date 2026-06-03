# Network Scripts

Scripts for parsing the Partner 40 schematic netlist and injecting nets into the KiCad PCB.

## Files

- `sch.txt` — hand-transcribed schematic netlist (source of truth)
- `nets.txt` — parsed netlist (generated, do not edit)
- `idp.net` — KiCad-format netlist for import (generated, do not edit)

## Workflow

### 1. Parse the schematic

Run from the `valid/` directory:

```
python parse_nets.py
```

Reads `sch.txt`, resolves synonyms and BANK expansions, writes `nets.txt`.

### 2. Generate the KiCad netlist

```
python gen_netlist.py
```

Reads `nets.txt`, applies pin swaps for specified CX capacitors, writes `idp.net`.

### 3. Import netlist into KiCad

In KiCad PCB editor: **File → Import → Netlist**, select `idp.net`.

This assigns nets to all footprint pads.

### 4. Propagate nets to traces and vias

In KiCad's scripting console (**Tools → Scripting Console**):

```python
exec(open(r"C:\Work\partner40\valid\propagate_nets.py").read())
```

This walks the physical connectivity from pads through connected tracks and vias,
assigning the correct net to each. Saves the PCB automatically.

### 5. Mark unconnected pads

In KiCad's scripting console:

```python
exec(open(r"C:\Work\partner40\valid\unconnected_pads.py").read())
```

Finds pads with no tracks connected. Assigns unique `unconnected-(REF-PadN)` nets to pads
with no net, and lists pads that have a net assigned but no physical trace.

## sch.txt Format

- `E4/3 M1I- E3/11` — component/pin pairs and net names on the same line belong to the same net
- `<E67>` — sets default component context; subsequent bare pin numbers (e.g. `5`) refer to `E67/5`
- `<>` — resets component context
- `-- LIST2` — section comment, ignored
- Multiple net names on one line (e.g. `DABUS11 DD1+`) are treated as synonyms for the same net
- `BANK1/5` expands to pin 5 of E59, E60, E61, E62, E71, E72, E73, E74
- `BANK2/5` expands to pin 5 of E85, E86, E87, E88, E98, E99, E100, E101
