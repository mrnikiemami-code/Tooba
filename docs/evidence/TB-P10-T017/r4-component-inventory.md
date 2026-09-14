# Component inventory — TB-P10-T017-R4

Unknown = 0. All customer-facing visuals map to PageShell / Section / Card / Elevated / Input / Interactive / Media / Header/Footer/Nav / Status / Composite / Decorative.

| Component | Used by routes | Current background source | Semantic class | Compliant? | Action |
| --- | --- | --- | --- | --- | --- |
| StorefrontShell | public storefront | `--color-page-background` | page | yes | keep |
| Storefront header/mega/mobile | public storefront | derived Card (`bg-surface`) | header / overlay | yes | tokenized |
| Storefront footer | public storefront | derived Card | footer | yes | tokenized |
| Product card + media well | Home/PLP/PDP/Landing | derived Card / Media | card / media | yes | role attrs |
| Home hero/stories/rails/brands/articles | `/` | Section* + derived cards | section/accent/alternate/card | yes | tokenized |
| PDP gallery/buy box/tabs/related | `/products/[slug]` | Media + Card + Alternate | media/card/alternate | yes | repaired |
| PLP/category filters | listing | derived Card | card | yes | tokenized |
| Cart/checkout/shipping/payment | commerce | Section* + Card/Input | section/card/input | yes | tokenized |
| Login OTP panel | `/login` | Elevated + Input | elevated/input | yes | tokenized |
| CustomerPanelShell | customer nav | page/header/elevated/section/overlay | same | yes | canvas + derived |
| Customer dashboard/orders/profile/settings/tickets/wallet | customer | derived Card | card | yes | tokenized |
| Blog cards/listings | `/blogs*` | derived Card | card | yes | tokenized |
| Decorative blobs / story chrome | cart/checkout/stories | Decorative | decorative | yes | classified |
| Status chips | orders/support | Status tokens | status | yes | keep |
| Enamad badges | footer | Media/brand | media | yes | keep |

Shared UI `support-ui` / customer `wallet-ui` consume the same tokens; Admin copies of those files keep Neutral paper because derived vars bind only on storefront/customer canvases.
