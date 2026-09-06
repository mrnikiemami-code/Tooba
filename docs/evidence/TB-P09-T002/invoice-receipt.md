# Invoice / receipt

`GET .../invoice.html` printable RTL HTML from CheckoutGroup line snapshots (unit/line totals, tax/discount seller totals, payment status). No settlement commission/payable. `GET .../receipt.html` when payment exists; masked reference, provider display name. FE opens via blob fetch with admin Actor header.
