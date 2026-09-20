# Locale Policy Plan

Centralize normalization, validation, supported-locale source, fallback chain, request/page/store locale.

Audit duplication of hardcoded `fa`/`en`, Trim, culture parsing across Host/modules.

Domain stores locale keys; does not select UI language.
