# 247 receipts

- Official V4 docs: `https://document.tipaxapi.org/#v4-docs`
- Official test OpenAPI: `https://omtestapi.tipax.ir/swagger/v4/swagger.json`
- Pricing route: `POST /api/OM/v4/Pricing`
- Request schema: `OmPriceDto` with `PackageInput`.
- Response schema: `ETipaxPriceMinListDto[]`, normalized from regular/plus/express/same-day/air rate slots.
- Token form fields: `username`, `password`, `grant_type=password`.
- Refresh route: `POST /api/OM/v4/Account/RefreshToken` with token and refreshToken.
- Focused test result: 4/4 passed.
- Adjacent structure/composition/captured-quote result: 27/27 passed.
- No real credential or provider request was used; HTTP behavior was proven with a controlled handler.
