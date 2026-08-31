# Smoke check (TB-P07-T040)

Contract/source verification (no lengthy browser campaign):

- Product list still imports AppDataGrid (reference unchanged)
- Admin GridPage + legacy screens import LegacyAppDataGrid, not DataGrid
- Attribute definitions + category schema + gift cards lost raw `<table>` markers
- Legacy bridge maps text/status filters to app-owned headers
