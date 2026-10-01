# boundary-microservice — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT

Across Access (12 files):

- foreign Application / Infrastructure / Domain: ZERO
- foreign DbContext / cross-module persistence / joins: ZERO
- RequestServices / service locator: ZERO
- module → Host dependency: ZERO

Allowed: BuildingBlocks seams, module Endpoints authorizer interfaces/codes, accepted Contracts.
Host Access owns only session/tenant/platform composition + thin auth adaptation — ZERO business command/domain/persistence authority.
