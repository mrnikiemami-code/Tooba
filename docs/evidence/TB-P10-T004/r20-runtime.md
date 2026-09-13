# R20 runtime matrix

Task IDs A–R:

| ID | Result |
| --- | --- |
| A Cart desktop active countdown | PASS |
| B Cart mobile active countdown | PASS |
| C fail payment, timer unchanged | PASS |
| D expired -> retry state | PASS |
| E retry cycle active | PASS |
| F max cycles state | PASS |
| G Manual AwaitingAdmin | PASS |
| H success removes pending card | PASS |
| I new Cart + old pending | PASS |
| J Admin Order Detail | PASS |
| K Orders grid | PASS |
| L Payments grid | PASS |
| M Store Settings | PASS |
| N Category override | PASS |
| O Offer override | PASS |
| P FA RTL | PASS |
| Q EN LTR | PASS |
| R network/console | PASS |

Customer matrix E (stock unavailable) also PASS via unpaid-retry 409 after temporary on_hand=0 (restored).

USER_VISUAL_ACCEPTED=NO
