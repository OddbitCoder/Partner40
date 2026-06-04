# Z80A CTC — Z8430 Counter/Timer Circuit

## Overview
The Z80A CTC (Zilog Z8430) is a four-channel counter/timer peripheral in a 28-pin DIP package, designed to interface directly with the Z80 CPU bus. Each of the four channels can be independently programmed as either a counter (counting external trigger events) or a timer (dividing the system clock), using a selectable prescaler of ÷16 or ÷256. When the internal downcounter reaches zero, a Zero Count/Timeout (ZC/TO) pulse is generated on three of the channels and an interrupt can be requested. Channel 3 has no ZC/TO output. The CTC supports the Z80 priority interrupt daisy-chain scheme via IEI and IEO pins. All active-low signals are denoted with a leading slash.

## Pinout

| Pin | Name | Type | Description |
|-----|------|------|-------------|
| 1 | D0 | I/O | System data bus bit 0 (3-state, bidirectional) |
| 2 | D1 | I/O | System data bus bit 1 (3-state, bidirectional) |
| 3 | D2 | I/O | System data bus bit 2 (3-state, bidirectional) |
| 4 | D3 | I/O | System data bus bit 3 (3-state, bidirectional) |
| 5 | D4 | I/O | System data bus bit 4 (3-state, bidirectional) |
| 6 | D5 | I/O | System data bus bit 5 (3-state, bidirectional) |
| 7 | D6 | I/O | System data bus bit 6 (3-state, bidirectional) |
| 8 | D7 | I/O | System data bus bit 7, MSB (3-state, bidirectional) |
| 9 | +5V | P | +5 V power supply |
| 10 | GND | P | Ground |
| 11 | CLK/TRG0 | I | External clock/timer trigger for channel 0; active edge is software-selectable (rising or falling) |
| 12 | CLK/TRG1 | I | External clock/timer trigger for channel 1 |
| 13 | CLK/TRG2 | I | External clock/timer trigger for channel 2 |
| 14 | CLK/TRG3 | I | External clock/timer trigger for channel 3 |
| 15 | CS0 | I | Channel select bit 0 — LSB of 2-bit binary address selecting one of four channels |
| 16 | CS1 | I | Channel select bit 1 — MSB of 2-bit binary address |
| 17 | /CE | I | Chip enable — enables CTC to accept or send data when low; active low |
| 18 | /RD | I | Read cycle status from Z80 CPU, active low |
| 19 | /IORQ | I | I/O request from Z80 CPU, active low |
| 20 | /M1 | I | Machine cycle 1 from Z80 CPU — used together with /IORQ for interrupt acknowledge |
| 21 | /RESET | I | Reset — immediately stops all channels and clears all control registers; active low |
| 22 | ZC/TO0 | O | Zero count/timeout output for channel 0 — pulses high for one CLK period when downcounter reaches zero |
| 23 | ZC/TO1 | O | Zero count/timeout output for channel 1 |
| 24 | ZC/TO2 | O | Zero count/timeout output for channel 2 |
| 25 | IEI | I | Interrupt enable in — active high; part of Z80 daisy-chain priority interrupt scheme |
| 26 | IEO | O | Interrupt enable out — active high; propagates interrupt enable to next device in daisy chain |
| 27 | /INT | OC | Interrupt request to Z80 CPU, open-drain, active low |
| 28 | CLK | I | System clock — single-phase input used to synchronise internal operations |

## Notes
- Channel 3 has a CLK/TRG3 input but no ZC/TO output; its only output is the shared /INT line.
- In timer mode, the CLK/TRG input starts the timer after a time-constant byte is written; in counter mode, each active edge of CLK/TRG decrements the downcounter.
- The prescaler divides the system clock by 16 or 256 before feeding the downcounter in timer mode.
- CS0/CS1 are typically connected to the two LSBs of the Z80 address bus (A0, A1); /CE is driven by address decoding logic combined with /IORQ.
- The ZC/TO outputs are commonly used to cascade channels (ZC/TO of one channel connected to CLK/TRG of the next) for extended time periods or baud-rate generation.
- Each channel can generate a unique interrupt vector based on the interrupt vector register, allowing the CPU to distinguish which channel caused the interrupt.
