# Z80A CPU — MK3880N-4 Central Processing Unit

## Overview
The Z80A CPU (Mostek MK3880N-4, equivalent to Zilog Z8400) is an 8-bit microprocessor in a 40-pin DIP package, operating at up to 4 MHz. It contains a 16-bit address bus capable of directly addressing 64 KB of memory, an 8-bit bidirectional data bus, and a full complement of control signals for memory, I/O, interrupt, and DMA operations. The CPU includes an internal refresh counter for DRAM refresh, two independent interrupt modes, a non-maskable interrupt, and a bus request/acknowledge mechanism for DMA transfers. All active-low signals are denoted with a leading slash.

## Pinout

| Pin | Name | Type | Description |
|-----|------|------|-------------|
| 1 | A11 | O | Address bus bit 11 (3-state) |
| 2 | A12 | O | Address bus bit 12 (3-state) |
| 3 | A13 | O | Address bus bit 13 (3-state) |
| 4 | A14 | O | Address bus bit 14 (3-state) |
| 5 | A15 | O | Address bus bit 15 (3-state) |
| 6 | CLK | I | System clock, single-phase MOS level |
| 7 | D4 | I/O | Data bus bit 4 (3-state, bidirectional) |
| 8 | D3 | I/O | Data bus bit 3 (3-state, bidirectional) |
| 9 | D5 | I/O | Data bus bit 5 (3-state, bidirectional) |
| 10 | D6 | I/O | Data bus bit 6 (3-state, bidirectional) |
| 11 | VCC | P | +5 V power supply |
| 12 | D2 | I/O | Data bus bit 2 (3-state, bidirectional) |
| 13 | D7 | I/O | Data bus bit 7, MSB (3-state, bidirectional) |
| 14 | D0 | I/O | Data bus bit 0, LSB (3-state, bidirectional) |
| 15 | D1 | I/O | Data bus bit 1 (3-state, bidirectional) |
| 16 | /INT | I | Maskable interrupt request, active low |
| 17 | /NMI | I | Non-maskable interrupt, active low, edge-triggered |
| 18 | /HALT | O | CPU is halted (executing NOP loop), active low |
| 19 | /MREQ | O | Memory request — address bus holds valid memory address, active low (3-state) |
| 20 | /IORQ | O | I/O request — address bus holds valid I/O port address, active low (3-state); also used for interrupt acknowledge when combined with /M1 |
| 21 | /RD | O | Read cycle — CPU wishes to read data from memory or I/O, active low (3-state) |
| 22 | /WR | O | Write cycle — CPU wishes to write data to memory or I/O, active low (3-state) |
| 23 | /BUSAK | O | Bus acknowledge — address, data, and control buses are in high-impedance state, active low |
| 24 | /WAIT | I | Wait state request — forces additional T-states, active low |
| 25 | /BUSRQ | I | Bus request — requests CPU to release buses, active low; recognized at end of current machine cycle |
| 26 | /RESET | I | Reset — initialises CPU and clears PC to 0000h, active low |
| 27 | /M1 | O | Machine cycle 1 — CPU is fetching an opcode, active low; used with /IORQ for interrupt acknowledge |
| 28 | /RFSH | O | Refresh — address bus A0–A6 hold DRAM refresh address, active low |
| 29 | GND | P | Ground |
| 30 | A0 | O | Address bus bit 0 (3-state) |
| 31 | A1 | O | Address bus bit 1 (3-state) |
| 32 | A2 | O | Address bus bit 2 (3-state) |
| 33 | A3 | O | Address bus bit 3 (3-state) |
| 34 | A4 | O | Address bus bit 4 (3-state) |
| 35 | A5 | O | Address bus bit 5 (3-state) |
| 36 | A6 | O | Address bus bit 6 (3-state) |
| 37 | A7 | O | Address bus bit 7 (3-state) |
| 38 | A8 | O | Address bus bit 8 (3-state) |
| 39 | A9 | O | Address bus bit 9 (3-state) |
| 40 | A10 | O | Address bus bit 10 (3-state) |

## Notes
- The MK3880N-4 is Mostek's 4 MHz version of the Z80A CPU, pin-compatible with the Zilog Z8400.
- During /HALT, the CPU executes NOP instructions and refreshes DRAM; an interrupt or reset is required to exit.
- The /RFSH signal combined with /MREQ (low) indicates a valid DRAM refresh cycle; lower 7 address bits (A0–A6) are used as the refresh row address.
- Bus request (/BUSRQ) has higher priority than /NMI; the CPU will not acknowledge /NMI while /BUSRQ is asserted.
- During interrupt acknowledge (IM 0 or IM 2), /M1 and /IORQ are both asserted simultaneously so that peripherals can place an interrupt vector on the data bus.
- The data bus bits are not laid out in numerical order on the package — D0–D7 are scattered across pins 7–15.
