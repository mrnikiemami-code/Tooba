# Template Inventory — TB-P10-T022-R13

- Host: http://127.0.0.1:5088
- Packs audited: 10
- All ok: true

| Key | Products | Roots | Brands | Banners | Pure | Disk media | Route | Apply |
| --- | ---: | ---: | ---: | ---: | --- | ---: | --- | --- |
| fashion | 15 | 8 | 6 | n/a | true | 8 | /template-preview/fashion | shared engine |
| auto-parts | 15 | 8 | 6 | n/a | true | 8 | /template-preview/auto-parts | shared engine |
| building-materials | 15 | 8 | 6 | n/a | true | 8 | /template-preview/building-materials | shared engine |
| tools-hardware | 15 | 8 | 6 | n/a | true | 8 | /template-preview/tools-hardware | shared engine |
| tile-ceramic | 15 | 8 | 6 | n/a | true | 8 | /template-preview/tile-ceramic | shared engine |
| interior-decor | 15 | 8 | 6 | n/a | true | 8 | /template-preview/interior-decor | shared engine |
| home-appliances | 15 | 8 | 6 | n/a | true | 8 | /template-preview/home-appliances | shared engine |
| shoes | 15 | 8 | 6 | n/a | true | 8 | /template-preview/shoes | shared engine |
| plants | 15 | 8 | 6 | n/a | true | 8 | /template-preview/plants | shared engine |
| beauty | 15 | 8 | 6 | n/a | true | 8 | /template-preview/beauty | shared engine |

## Schema parity notes

- Exactly 1 StoreTemplate per key (Host seed + runtime preview API).
- Exactly 8 top-level category roots; 3-level trees proven by Batch A/B/C Host tests (2 mid × 2 leaf per root).
- Exactly 15 TemplateProducts; brands + banners + Persian translations present.
- Deterministic preview routes `/template-preview/{key}` (+ `/full`).
- Template Apply uses shared FE materialization (`buildTemplateSectionPayloads`) — no Catalog mutation.
- Seed idempotency retained via Industry Batch A/B/C + Fashion seeds.

## Per-pack detail

### fashion

- templateId: 019022a5-0000-7000-8000-00000000f001
- mediaPrefix: /images/fashion-template/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### auto-parts

- templateId: 019022b1-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-auto-parts/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### building-materials

- templateId: 019022b3-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-building-materials/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### tools-hardware

- templateId: 019022b5-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-tools-hardware/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### tile-ceramic

- templateId: 019022b7-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-tile-ceramic/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### interior-decor

- templateId: 019022b9-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-interior-decor/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### home-appliances

- templateId: 019022bb-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-home-appliances/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### shoes

- templateId: 019022bd-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-shoes/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### plants

- templateId: 019022bf-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-plants/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true

### beauty

- templateId: 019022c1-0000-7000-8000-00000000f001
- mediaPrefix: /images/template-beauty/
- operational hits: product=0 category=0 brand=0
- foreignMedia: none
- ok: true
