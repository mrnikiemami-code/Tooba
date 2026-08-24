# TB-P04-T005 visual repair 3 — overflow check

Predicate: `document.documentElement.scrollWidth <= document.documentElement.clientWidth`.

Measured via CDP `Runtime.evaluate` after `Emulation.setDeviceMetricsOverride`.

| route | CSS viewport | innerWidth | clientWidth | scrollWidth | page overflow | notes |
| --- | --- | --- | --- | --- | --- | --- |
| `/admin/products` | 1440×900 | 1440 | 1440 | 1440 | false | `input[type=range]` count = 0 in default list |
| `/admin/products/{id}` overview | 390×844 | 390 | 390 | 390 | false | hamburger `باز کردن منو` present |
| `/admin/products/{id}` commercial | 390×844 | 390 | 390 | 390 | false | section combobox `فروش و قیمت` |

Desktop list default view has no page-level horizontal scrollbar.
