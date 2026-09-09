# Grid final — TB-P09-T017

AppDataGrid canonical. Order number is text; only View navigates. `تعداد اقلام` = header `TotalItemCount`. Status/payment humanized. Filters/sort intact.

Repair: `orderStatusEnumOptions` now includes ReturnRequested/Approved and RefundPending/Completed/Failed. Dead `Processing` option removed. Backend `ApplyStatusFilterAsync` matches overlay return/refund composed statuses.
