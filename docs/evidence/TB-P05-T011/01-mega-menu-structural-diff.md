# Mega Menu structural diff

Primary source: purchased Shopeiva
`D:/Users/User/source/repos/SarvNewVerRequirment/reference/shopeiva/src/components/common/Header/Header.jsx`
desktop lines 413–577 and mobile lines 688–745.

| Area | Original Shopeiva | Tooba before repair | Classification | Repair |
| --- | --- | --- | --- | --- |
| Root panel | full-width band under header, top border, no floating outer radius | 1100px floating rounded card aligned to trigger | VISUAL REGRESSION | restored full-width `left-0 right-0 top-full` band |
| Interior | max-1800 12-column, 3/6/3 split, gap 6 | 3/6/3 but tighter gap and floating padding | VISUAL REGRESSION | restored original container, proportions, padding and gap |
| Category rail | heading bar, “همه”, icon boxes, active chevron, themed scroll | plain text buttons without heading/icons | VISUAL REGRESSION | ported original hierarchy/chrome with live roots |
| Detail pane | bordered middle pane, heading icon, view-all, two-column hierarchy | generic two-column block plus non-source chips | VISUAL REGRESSION | ported original pane; removed custom chips |
| Hierarchy depth | subcategory headings plus leaf items | live two/three-level tree | DATA-BINDING DIFFERENCE | child categories and real deeper descendants only; no fake leaves |
| Promo | gradient Gift card plus brands block | one generic blue card | VISUAL REGRESSION | restored two-block placement; honest offers CTA and live brands |
| Secondary nav | icon links after vertical separator | root categories as plain links | VISUAL REGRESSION | restored source navigation-strip pattern with existing public routes |
| Hover | 150ms enter/leave stability and scroll-close | immediate close/open | VISUAL REGRESSION | restored timeout and scroll-close |
| Accent | Shopeiva red | Tooba blue | INTENTIONAL APPROVED DIFFERENCE | blue retained without geometry changes |
| Routes/data | static Shopeiva JSON demo | Host Catalog category IDs | DATA-BINDING DIFFERENCE | live Tooba URLs retained |
| Mobile | right drawer, main accordion, nested root accordions, 2-col children | flat root/child list | VISUAL REGRESSION | original accordion and 2-column child grid restored |
| Dark mode | source has dark variants | current Tooba storefront is light-only | FRAMEWORK ADAPTATION | no new theme scope introduced |

No product, price, stock, seller, rating, or discount data is rendered in the
menu. The original “up to 50%” claim was deliberately not copied because no
backend campaign contract authorizes it.
