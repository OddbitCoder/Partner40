# Z80A DMA — Z8410 Direct Memory Access Controller

## Overview
The Z80A DMA (Zilog Z8410) is a single-channel DMA controller in a 40-pin DIP package that can transfer data between any combination of memory and I/O devices without CPU intervention. It has its own 16-bit address counter, byte counter, and a fully programmable set of transfer modes: memory-to-memory, memory-to-I/O, I/O-to-memory, and I/O-to-I/O. The DMA supports two operating modes: burst mode (holds the bus until the transfer is complete) and cycle-steal mode (releases the bus after each byte). It uses the Z80 bus request/acknowledge mechanism to take control of the system buses and integrates into the Z80 priority interrupt daisy chain. All active-low signals are denoted with a leading slash.

## Pinout

| Pin | Name | Type | Description |
|-----|------|------|-------------|
| 1 | A5 | O | Address bus bit 5 (3-state) |
| 2 | A4 | O | Address bus bit 4 (3-state) |
| 3 | A3 | O | Address bus bit 3 (3-state) |
| 4 | A2 | O | Address bus bit 2 (3-state) |
| 5 | A1 | O | Address bus bit 1 (3-state) |
| 6 | A0 | O | Address bus bit 0 (3-state) |
| 7 | CLK | I | System clock, single-phase |
| 8 | /WR | I/O | Write — input from CPU during programming; output during DMA write cycles (3-state) |
| 9 | /RD | I/O | Read — input from CPU during programming; output during DMA read cycles (3-state) |
| 10 | /IORQ | I/O | I/O request — input from CPU; driven as output during DMA I/O transfers (3-state) |
| 11 | VCC | P | +5 V power supply |
| 12 | /MREQ | I/O | Memory request — input from CPU; driven as output during DMA memory transfers (3-state) |
| 13 | /BAO | O | Bus acknowledge out — active low; in multi-DMA daisy chain, signals that this DMA has or is passing bus control |
| 14 | /BAI | I | Bus acknowledge in — active low; CPU /BUSAK connects here (highest-priority DMA) or to /BAO of previous DMA |
| 15 | /BUSRQ | O | Bus request to CPU — active low; asserted when DMA needs bus; released in cycle-steal mode after each transfer |
| 16 | /CE /WAIT | I | Chip enable / wait — active low; selects DMA for programming by CPU; in some modes can also function as a WAIT input |
| 17 | A15 | O | Address bus bit 15 (3-state) |
| 18 | A14 | O | Address bus bit 14 (3-state) |
| 19 | A13 | O | Address bus bit 13 (3-state) |
| 20 | A12 | O | Address bus bit 12 (3-state) |
| 21 | A11 | O | Address bus bit 11 (3-state) |
| 22 | A10 | O | Address bus bit 10 (3-state) |
| 23 | A9 | O | Address bus bit 9 (3-state) |
| 24 | A8 | O | Address bus bit 8 (3-state) |
| 25 | RDY | I | Ready — used in burst mode; when RDY goes inactive the DMA releases the bus (/BUSRQ deasserted) |
| 26 | /M1 | I | Machine cycle 1 from Z80 CPU — used with /IORQ for interrupt acknowledge |
| 27 | D7 | I/O | Data bus bit 7, MSB (3-state, bidirectional) |
| 28 | D6 | I/O | Data bus bit 6 (3-state, bidirectional) |
| 29 | D5 | I/O | Data bus bit 5 (3-state, bidirectional) |
| 30 | GND | P | Ground |
| 31 | D4 | I/O | Data bus bit 4 (3-state, bidirectional) |
| 32 | D3 | I/O | Data bus bit 3 (3-state, bidirectional) |
| 33 | D2 | I/O | Data bus bit 2 (3-state, bidirectional) |
| 34 | D1 | I/O | Data bus bit 1 (3-state, bidirectional) |
| 35 | D0 | I/O | Data bus bit 0, LSB (3-state, bidirectional) |
| 36 | IEO | O | Interrupt enable out — active high; daisy-chain interrupt priority output |
| 37 | /INT /PULSE | OC | Interrupt request to CPU (open-drain, active low); also used as a pulse output to signal end-of-block transfer |
| 38 | IEI | I | Interrupt enable in — active high; daisy-chain interrupt priority input |
| 39 | A7 | O | Address bus bit 7 (3-state) |
| 40 | A6 | O | Address bus bit 6 (3-state) |

## Notes
- The DMA outputs its own address (A0–A15) onto the system address bus during transfers, independently of the CPU.
- /BAI and /BAO form the daisy-chain bus priority scheme: the device with /BAI tied to CPU /BUSAK has highest DMA priority; lower-priority DMAs chain via /BAO → /BAI.
- In burst mode, the DMA holds /BUSRQ asserted for the entire block transfer; in cycle-steal mode it releases and re-requests the bus for each byte, allowing the CPU to interleave.
- RDY is used by a slow peripheral to pause a burst transfer; when RDY is deasserted the DMA releases the bus.
- The /CE//WAIT pin is normally used as chip enable (active low) for programming the DMA registers via the CPU; it can also be programmed to act as an external WAIT signal during transfers.
- The DMA is programmed by writing a sequence of register bytes using the CPU's I/O write instructions with /CE asserted; the register set is write-only except for the status register.
- The /INT//PULSE output can be configured as either an interrupt request or a pulse at end-of-block, selectable via the interrupt control register.
