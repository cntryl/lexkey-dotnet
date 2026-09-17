# LexKey encoding specification

This repository implements the same portable byte contract as `cntryl/lexkey-rs`.
Lexicographic order always means unsigned byte-wise comparison.

## Typed values

| Value | Encoding |
|---|---|
| string / bytes | raw bytes |
| UUID | 16 RFC-4122 network-order bytes |
| Boolean | false `00`, true `01` |
| unsigned integer | declared-width big-endian |
| signed integer | declared-width sign bit flipped, then big-endian |
| float | negative IEEE bits inverted; non-negative sign bit flipped; NaN rejected |
| Unix nanoseconds | signed 64-bit encoding |
| nil | `00` |
| end marker | `ff` |

Integer and floating-point ordering is guaranteed within the same declared width. Schemas
that need cross-width equality or ordering must normalize to an explicit common width.

## Composites

Encoded parts are concatenated with one `00` separator between adjacent parts and no
trailing separator. Empty parts are retained and may produce adjacent separators.

## Ranges

- First structured child: `P || 00`.
- Last structured child: `P || ff` (exclusive).
- Row lower bound `L`: `P || 00 || L`.
- Row upper bound `U`: `P || 00 || U || ff`.
- Raw prefix successor: increment the final byte not equal to `ff`, then truncate. Empty
  and all-`ff` prefixes have no finite successor.

`P || ff` is only the structured upper bound for children encoded through the normal
separator path. It is not a general raw-prefix successor.
