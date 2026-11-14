/**
 * نگاشت پایدار کد خطای Admin به پیام انسان‌خوان fa/en.
 * متن خام HTTP / Bad Request / stack هرگز به UI برنمی‌گردد.
 */

export type AdminErrorLocale = "fa" | "en";

const UNKNOWN_FA = "خطایی رخ داد. لطفاً دوباره تلاش کنید.";
const UNKNOWN_EN = "Something went wrong. Please try again.";

/** پیام‌های شناخته‌شده — کلید = errorCode پایدار Host. */
const ADMIN_ERROR_MESSAGES: Record<string, { fa: string; en: string }> = {
  "catalog.attribute.name.duplicate": {
    fa: "ویژگی‌ای با این نام قبلاً وجود دارد.",
    en: "An attribute with this name already exists.",
  },
  "catalog.attribute.code.duplicate": {
    fa: "این کد ویژگی قبلاً استفاده شده است.",
    en: "This attribute code is already in use.",
  },
  "catalog.attribute.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.attribute.variant_axis.value_kind.invalid": {
    fa: "این نوع ویژگی برای ساخت تنوع مناسب نیست.",
    en: "This attribute type cannot be used for variants.",
  },
  "catalog.attribute.variant_axis.capability_disabled": {
    fa: "امکان استفاده از این ویژگی برای تنوع در تعریف اصلی آن فعال نشده است.",
    en: "Variant axis capability is not enabled on this attribute definition.",
  },
  "catalog.attribute.variant_axis.in_use": {
    fa: "این ویژگی در تنوع‌های فعال استفاده می‌شود.",
    en: "This attribute is used in active variants.",
  },
  "catalog.category.attribute.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.schema.invalid": {
    fa: "تنظیم ویژگی برای این دسته معتبر نیست.",
    en: "This attribute setting is not valid for the category.",
  },
  "catalog.schema.duplicate-inherited": {
    fa: "این ویژگی از قبل از دسته والد به ارث رسیده است. در صورت نیاز تنظیمات استفاده آن را برای این دسته تغییر دهید.",
    en: "This attribute is already inherited from a parent category. Customize its usage for this category if needed.",
  },
  "catalog.facet.invalid": {
    fa: "تنظیم فیلتر معتبر نیست. ویژگی باید برای این دسته قابل فیلتر باشد.",
    en: "Filter settings are not valid. The attribute must be filterable for this category.",
  },
  "catalog.facet.missing": {
    fa: "تنظیم فیلتر یافت نشد.",
    en: "Filter settings were not found.",
  },
  "catalog.category.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.category.depth.exceeded": {
    fa: "عمیق‌تر از سطح سوم برای دسته‌بندی مجاز نیست.",
    en: "Categories deeper than level 3 are not allowed.",
  },
  "catalog.category.assignment.duplicate": {
    fa: "این دسته قبلاً به محصول اضافه شده است.",
    en: "This category is already assigned to the product.",
  },
  "catalog.category.assignment.duplicate_primary": {
    fa: "این دسته هم‌اکنون دسته اصلی محصول است.",
    en: "This category is already the product's primary category.",
  },
  "catalog.category.assignment.cannot_remove_primary": {
    fa: "دسته اصلی را نمی‌توان مستقیم حذف کرد؛ ابتدا دسته اصلی دیگری انتخاب کنید.",
    en: "The primary category cannot be removed directly; choose another primary first.",
  },
  "catalog.category.assignment.missing": {
    fa: "پیوند دسته برای این محصول یافت نشد.",
    en: "Category assignment was not found for this product.",
  },
  "catalog.category.assignment.invalid": {
    fa: "اختصاص دسته معتبر نیست.",
    en: "Category assignment is not valid.",
  },
  "catalog.category.assignment.level.invalid": {
    fa: "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
    en: "A product must be assigned to a level-3 category.",
  },
  "catalog.category.assignment.stale": {
    fa: "این مورد را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.",
    en: "Someone else changed this record. Reload the latest version.",
  },
  "catalog.category.slug.duplicate": {
    fa: "این نامک برای یک دسته‌بندی دیگر استفاده شده است. یک نامک متفاوت انتخاب کنید.",
    en: "This slug is already used by another category. Choose a different slug.",
  },
  "catalog.product.category.level.invalid": {
    fa: "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
    en: "A product must be assigned to a level-3 category.",
  },
  "workspace.product.category.level.invalid": {
    fa: "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
    en: "A product must be assigned to a level-3 category.",
  },
  "workspace.product.category.invalid": {
    fa: "ردهٔ انتخاب‌شده معتبر نیست.",
    en: "The selected category is not valid.",
  },
  "workspace.product.category.schema-impact": {
    fa: "تغییر دسته بر ویژگی‌ها یا تنوع‌ها اثر می‌گذارد؛ تأیید صریح لازم است.",
    en: "Changing category affects attributes or variants; explicit confirmation is required.",
  },
  "catalog.variant.invalid": {
    fa: "تنظیم تنوع معتبر نیست.",
    en: "Variant settings are not valid.",
  },
  "catalog.variant.preview.invalid": {
    fa: "پیش‌نمایش تنوع‌ها معتبر نیست.",
    en: "Variant preview is not valid.",
  },
  "catalog.variant.apply.invalid": {
    fa: "اعمال تنوع‌ها انجام نشد.",
    en: "Could not apply variant changes.",
  },
  "catalog.variant.readiness.invalid": {
    fa: "وضعیت آمادگی تنوع‌ها معتبر نیست.",
    en: "Variant readiness is not valid.",
  },
  "catalog.variant_axes.invalid": {
    fa: "ویژگی تنوع انتخاب‌شده معتبر نیست.",
    en: "The selected variant attribute is not valid.",
  },
  "catalog.variant.editor.parse": {
    fa: "بارگذاری ویرایشگر تنوع‌ها ناموفق بود.",
    en: "Could not load the variant editor.",
  },
  "catalog.variant.preview.parse": {
    fa: "پیش‌نمایش تنوع‌ها ناموفق بود.",
    en: "Could not preview variants.",
  },
  "catalog.variant.apply.parse": {
    fa: "ذخیره تنوع‌ها ناموفق بود.",
    en: "Could not save variants.",
  },
  "workspace.variant.axes.missing": {
    fa: "ویژگی تنوع انتخاب نشده است.",
    en: "No variant attribute is selected.",
  },
  "workspace.variant.create.rejected": {
    fa: "ساخت تنوع انجام نشد.",
    en: "Variant creation was rejected.",
  },
  "workspace.variant.missing": {
    fa: "تنوع یافت نشد.",
    en: "Variant was not found.",
  },
  "workspace.variant.status.invalid": {
    fa: "وضعیت تنوع معتبر نیست.",
    en: "Variant status is not valid.",
  },
  "workspace.product.category.assign.rejected": {
    fa: "اختصاص دسته انجام نشد.",
    en: "Category assignment was rejected.",
  },
  "workspace.product.category-failed": {
    fa: "اختصاص دسته انجام نشد.",
    en: "Category assignment failed.",
  },
  "workspace.product.brand.invalid": {
    fa: "برند انتخاب‌شده معتبر نیست.",
    en: "The selected brand is not valid.",
  },
  "workspace.product.brand-failed": {
    fa: "انتساب برند انجام نشد.",
    en: "Brand assignment failed.",
  },
  "workspace.catalog.stale": {
    fa: "این مورد را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.",
    en: "Someone else changed this record. Reload the latest version.",
  },
  "workspace.permission.denied": {
    fa: "دسترسی مجاز نیست.",
    en: "You are not allowed to perform this action.",
  },
  "workspace.product.missing": {
    fa: "محصول یافت نشد.",
    en: "Product was not found.",
  },
  "workspace.host.unreachable": {
    fa: "اتصال به سرویس برقرار نیست. لطفاً دوباره تلاش کنید.",
    en: "Could not reach the service. Please try again.",
  },
  "workspace.product.create-failed": {
    fa: "ایجاد محصول ناموفق بود.",
    en: "Product creation failed.",
  },
  "workspace.product.core-failed": {
    fa: "ذخیرهٔ اطلاعات محصول ناموفق بود.",
    en: "Saving product details failed.",
  },
  "workspace.physical_specification.rejected": {
    fa: "مشخصات فیزیکی نامعتبر است. وزن باید عدد صحیح گرم باشد؛ ابعاد (سانتی‌متر) در صورت ورود باید مثبت باشند.",
    en: "Physical specification is invalid. Weight must be a whole number of grams; dimensions (cm) must be positive when provided.",
  },
  "workspace.quantity.rejected": {
    fa: "سیاست مقدار نامعتبر است.",
    en: "Quantity policy is invalid.",
  },
  "admin.authorization.denied": {
    fa: "دسترسی مجاز نیست.",
    en: "You are not allowed to perform this action.",
  },
  "order.operation.denied": {
    fa: "مجوز انجام این عملیات وجود ندارد.",
    en: "You are not allowed to perform this order operation.",
  },
  "order.operation.invalid": {
    fa: "این عملیات در وضعیت فعلی سفارش مجاز نیست.",
    en: "This operation is not valid for the current order state.",
  },
  "admin.order.missing": {
    fa: "سفارشی با این شناسه پیدا نشد. ممکن است حذف شده باشد یا نشانی آن درست نباشد.",
    en: "No order was found with this identifier; it may have been removed or the address is wrong.",
  },
  "order.operation.failed": {
    fa: "انجام عملیات سفارش ناموفق بود.",
    en: "The order operation could not be completed.",
  },
  "order.shipment.accepted_group_required": {
    fa: "قبل از ایجاد مرسوله باید گروه ارسال پذیرفته‌شده را مشخص کنید.",
    en: "Select an accepted delivery group before creating a parcel.",
  },
  "order.shipment.accepted_group_unavailable": {
    fa: "این سفارش شاهد گروه ارسال پذیرفته‌شده ندارد؛ ایجاد مرسوله در دسترس نیست.",
    en: "This order has no accepted delivery-group evidence; parcel create is unavailable.",
  },
  "order.shipment.accepted_group_foreign": {
    fa: "گروه ارسال انتخاب‌شده متعلق به این سفارش فروشنده نیست.",
    en: "The selected delivery group does not belong to this seller order.",
  },
  "order.shipment.accepted_group_method_mismatch": {
    fa: "روش ارسال نمی‌تواند با تعهد پذیرفته‌شدهٔ تحویل متفاوت باشد.",
    en: "Shipping method cannot differ from the accepted delivery commitment.",
  },
  "fulfillment.shipment.shipping_group_method_mismatch": {
    fa: "روش ارسال با گروه پذیرفته‌شدهٔ این سفارش یکی نیست.",
    en: "Shipping method does not match this order's accepted delivery group.",
  },
  "order.shipment.accepted_group_line_foreign": {
    fa: "یک یا چند قلم خارج از گروه ارسال پذیرفته‌شده است.",
    en: "One or more lines are outside the accepted delivery group.",
  },
  "order.shipment.accepted_group_quantity_excess": {
    fa: "تعداد درخواستی از مقدار بسته‌بندی‌شدهٔ باقی‌ماندهٔ این گروه بیشتر است.",
    en: "Requested quantity exceeds remaining packed quantity for this group.",
  },
  "order.shipment.accepted_group_not_executable": {
    fa: "در وضعیت فعلی سفارش، ایجاد مرسوله برای این گروه ارسال ممکن نیست.",
    en: "This delivery group cannot create a parcel in the current order state.",
  },
  "order.shipment.carrier_api_use_dedicated_path": {
    fa: "این گروه حامل API است؛ ایجاد مرسوله فقط از پنل اجرای حامل همین گروه مجاز است.",
    en: "This group uses carrier API; create the parcel only from the group carrier execution panel.",
  },
  "fulfillment.shipping.post.recipient_required": {
    fa: "ارسال پستی نیازمند نام گیرنده است.",
    en: "Postal shipping requires a recipient name.",
  },
  "fulfillment.shipping.post.address_required": {
    fa: "ارسال پستی نیازمند نشانی گیرنده است.",
    en: "Postal shipping requires a destination address.",
  },
  "fulfillment.shipping.courier.address_required": {
    fa: "پیک آنلاین نیازمند نشانی مبدأ و مقصد معتبر است؛ مبدأ را در تنظیمات مبدأ/فرستنده تکمیل کنید.",
    en: "Online courier requires both origin and destination addresses; complete the seller origin/sender setup.",
  },
  "fulfillment.shipping.pickup.location_required": {
    fa: "تحویل حضوری نیازمند محل دریافت مشخص است؛ محل تحویل را در تنظیمات روش تکمیل کنید.",
    en: "In-person pickup requires a configured collection location.",
  },
  "fulfillment.shipping_method.unsupported": {
    fa: "این روش ارسال برای ایجاد مرسوله پشتیبانی نمی‌شود.",
    en: "This shipping method is not supported for parcel create.",
  },
  "order.shipment.provider_metadata_forbidden": {
    fa: "فراسنج‌های ارائه‌دهنده برای ایجاد مرسولهٔ دستی پذیرفته نمی‌شود.",
    en: "Provider metadata is not accepted for manual parcel create.",
  },
  "payment.restore.not_manual": {
    fa: "فقط پرداخت کارت‌به‌کارت/دستی قابل بازگردانی است.",
    en: "Only manual or card-to-card payments can be restored.",
  },
  "payment.restore.already_succeeded": {
    fa: "پرداخت موفق جایگزین شده و قابل بازگردانی نیست.",
    en: "A successful payment already exists and cannot be restored.",
  },
  "payment.restore.invalid_state": {
    fa: "بازگرداندن واریز در این وضعیت مجاز نیست.",
    en: "Deposit restore is not allowed in the current payment state.",
  },
  "payment.unconfirm.not_manual": {
    fa: "فقط پرداخت کارت‌به‌کارت/دستی قابل برگشت از واریز است.",
    en: "Only manual or card-to-card payments can undo a deposit.",
  },
  "payment.unconfirm.invalid_state": {
    fa: "برگشت از واریز در این وضعیت مجاز نیست.",
    en: "Undo deposit is not allowed in the current payment state.",
  },
  "payment.unconfirm.irreversible": {
    fa: "برگشت از واریز پس از ارسال، تحویل یا بازگشت وجه تکمیل‌شده مجاز نیست.",
    en: "Undo deposit is not allowed after dispatch, delivery, or a completed refund.",
  },
  "payment.unconfirm.payout_completed": {
    fa: "برگشت از واریز پس از واریز سهم فروشنده مجاز نیست.",
    en: "Undo deposit is not allowed after the seller share has been paid out.",
  },
  "fulfillment.unconfirm.already_started": {
    fa: "پس از شروع پردازش نمی‌توان واریز را برگرداند.",
    en: "Deposit cannot be undone after fulfillment has started.",
  },
  "order.payment.unconfirm.invalid_state": {
    fa: "وضعیت سفارش برای برگشت از واریز مناسب نیست.",
    en: "The order state does not allow undoing the deposit.",
  },
  "settlement.unconfirm.payout_completed": {
    fa: "برگشت از واریز پس از واریز سهم فروشنده مجاز نیست.",
    en: "Undo deposit is not allowed after the seller share has been paid out.",
  },
  "order.restore.not_cancelled": {
    fa: "فقط سفارش لغوشده را می‌توان بازگرداند.",
    en: "Only a cancelled order can be restored.",
  },
  "order.restore.missing_snapshot": {
    fa: "وضعیت قبل از لغو برای بازگردانی موجود نیست.",
    en: "The pre-cancel status snapshot is missing.",
  },
  "order.restore.refund_completed": {
    fa: "بازگردانی پس از بازگشت وجه تکمیل‌شده مجاز نیست.",
    en: "Restore is not allowed after a completed refund.",
  },
  "order.restore.delivered": {
    fa: "بازگردانی پس از تحویل مجاز نیست؛ از مرجوعی استفاده کنید.",
    en: "Restore is not allowed after delivery. Use a return instead.",
  },
  "order.restore.dispatched": {
    fa: "بازگردانی پس از ارسال مرسوله مجاز نیست.",
    en: "Restore is not allowed after a shipment has been dispatched.",
  },
  "fulfillment.pack.after_delivered": {
    fa: "پس از تحویل کامل نمی‌توان بسته‌بندی کرد.",
    en: "Packing is not allowed after full delivery.",
  },
  "fulfillment.process.after_delivered": {
    fa: "پس از تحویل کامل نمی‌توان پردازش را ادامه داد.",
    en: "Processing cannot continue after full delivery.",
  },
  "fulfillment.pack.requires_processing": {
    fa: "ابتدا پردازش را شروع کنید.",
    en: "Start processing before packing.",
  },
  "fulfillment.pack.not_processing": {
    fa: "این قلم هنوز در مرحله پردازش نیست.",
    en: "This line is not in the processing stage yet.",
  },
  "fulfillment.ship.not_packed": {
    fa: "این قلم هنوز بسته‌بندی نشده است.",
    en: "This line has not been packed yet.",
  },
  "fulfillment.selection.qty_exceeded": {
    fa: "تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.",
    en: "The selected quantity is larger than the operable quantity.",
  },
  "fulfillment.bulk.incompatible": {
    fa: "ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.",
    en: "The selected rows are not compatible for this action.",
  },
  "fulfillment.bulk.cross_seller": {
    fa: "عملیات گروهی روی فروشندگان متفاوت مجاز نیست.",
    en: "Bulk actions cannot mix sellers.",
  },
  "order.cancelled.blocks_action": {
    fa: "سفارش لغوشده است؛ این عملیات مجاز نیست.",
    en: "This order is cancelled; the action is not allowed.",
  },
  "order.cancel.forbidden": {
    fa: "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.",
    en: "The whole order cannot be cancelled after goods have been dispatched.",
  },
  "order.restore.seller_payout_completed": {
    fa: "این سفارش به‌دلیل انجام تسویه/واریز سهم فروشنده قابل بازگردانی نیست.",
    en: "This order cannot be restored because the seller share has already been paid out.",
  },
  "order.restore.inventory_failed": {
    fa: "بازگردانی ممکن نیست؛ موجودی برای رزرو دوباره کافی نیست. سفارش لغوشده باقی ماند.",
    en: "Restore failed; inventory could not be reserved again. The order stayed cancelled.",
  },
  "order.restore.invalid_state": {
    fa: "بازگردانی سفارش در این وضعیت مجاز نیست.",
    en: "Order restore is not allowed in the current state.",
  },
  "fulfillment.tracking.locked_after_dispatch": {
    fa: "پس از ارسال نمی‌توان کد رهگیری را اصلاح کرد.",
    en: "Tracking cannot be corrected after dispatch.",
  },
  "fulfillment.tracking.nothing_to_correct": {
    fa: "ابتدا کد رهگیری را ثبت کنید.",
    en: "Assign tracking before correcting it.",
  },
  "fulfillment.tracking.invalid_state": {
    fa: "اصلاح کد رهگیری در این وضعیت مرسوله مجاز نیست.",
    en: "Tracking correction is not allowed in the current shipment state.",
  },
  "fulfillment.work_queue.cross_seller": {
    fa: "انتخاب چندفروشنده برای عملیات گروهی مجاز نیست.",
    en: "Cross-seller bulk selection is not allowed.",
  },
  "fulfillment.work_queue.incompatible": {
    fa: "انتخاب ناسازگار است؛ همهٔ ردیف‌ها باید همان عملیات مجاز را داشته باشند.",
    en: "Incompatible selection; all rows must share the same allowed action.",
  },
  "fulfillment.work_queue.bulk_unsupported": {
    fa: "این عملیات گروهی پشتیبانی نمی‌شود.",
    en: "This bulk action is not supported.",
  },
  "fulfillment.work_queue.bulk_empty": {
    fa: "هیچ ردیفی برای عملیات گروهی انتخاب نشده است.",
    en: "No rows were selected for the bulk action.",
  },
  "fulfillment.work_queue.bulk_failed": {
    fa: "عملیات گروهی ارسال ناموفق بود.",
    en: "The fulfillment bulk operation failed.",
  },
  "fulfillment.work_queue.shipment_missing": {
    fa: "مرسولهٔ معتبر برای این عملیات یافت نشد.",
    en: "No valid shipment was found for this operation.",
  },
  "fulfillment.work_queue.row_mismatch": {
    fa: "ردیف انتخاب‌شده با دادهٔ سرور هم‌خوان نیست.",
    en: "The selected row does not match server data.",
  },
  "fulfillment.missing": {
    fa: "ارسال یافت نشد.",
    en: "Fulfillment was not found.",
  },
  "fulfillment.order.not_found": {
    fa: "سفارش مرتبط با این ارسال یافت نشد.",
    en: "The seller order behind this fulfillment was not found.",
  },
  "fulfillment.order.payment_not_authorized": {
    fa: "این سفارش هنوز پرداخت/تأمین تأییدشده ندارد؛ آماده‌سازی و ارسال فیزیکی پس از تأیید پرداخت ممکن است.",
    en: "This order has no confirmed payment/supply authorization yet; physical preparation and dispatch require payment confirmation.",
  },
  "fulfillment.dispatch.invalid_state": {
    fa: "ارسال در وضعیت فعلی مرسوله مجاز نیست.",
    en: "Dispatch is not allowed in the current shipment state.",
  },
  "fulfillment.dispatch.tracking_required": {
    fa: "بدون کد رهگیری نمی‌توان ارسال کرد.",
    en: "A tracking reference is required before dispatch.",
  },
  "fulfillment.tracking.duplicate": {
    fa: "این کد رهگیری قبلاً ثبت شده است.",
    en: "This tracking code is already registered.",
  },
  "fulfillment.dispatch.already_dispatched": {
    fa: "این مرسوله قبلاً ارسال شده است.",
    en: "This shipment has already been dispatched.",
  },
  "fulfillment.shipment.void_after_dispatch": {
    fa: "پس از ارسال نمی‌توان مرسوله را ابطال کرد.",
    en: "A shipment cannot be voided after dispatch.",
  },
  "fulfillment.shipment.void_invalid_state": {
    fa: "ابطال مرسوله در این وضعیت مجاز نیست.",
    en: "The shipment cannot be voided in the current state.",
  },
  "fulfillment.nondelivery.reason_required": {
    fa: "دلیل عدم تحویل را انتخاب کنید.",
    en: "Select a non-delivery reason.",
  },
  "fulfillment.nondelivery.reason_invalid": {
    fa: "دلیل عدم تحویل معتبر نیست.",
    en: "The non-delivery reason is not valid.",
  },
  "fulfillment.nondelivery.note_invalid": {
    fa: "توضیح عدم تحویل بیش از حد مجاز است.",
    en: "The non-delivery note is too long.",
  },
  "fulfillment.nondelivery.after_delivered": {
    fa: "برای مرسولهٔ تحویل‌شده عدم تحویل ثبت نمی‌شود.",
    en: "A delivered parcel cannot be marked non-delivered.",
  },
  "fulfillment.nondelivery.invalid_status": {
    fa: "ثبت عدم تحویل در وضعیت فعلی مرسوله مجاز نیست.",
    en: "Non-delivery is not allowed in the current parcel state.",
  },
  "fulfillment.nondelivery.already_returning": {
    fa: "این مرسوله در مسیر برگشت به مبدأ است.",
    en: "This parcel is already returning to origin.",
  },
  "fulfillment.nondelivery.conflict": {
    fa: "این مرسوله پیش‌تر به‌عنوان عدم تحویل ثبت شده است.",
    en: "This parcel is already recorded as non-delivered.",
  },
  "fulfillment.return.invalid_status": {
    fa: "شروع برگشت در وضعیت فعلی مرسوله مجاز نیست.",
    en: "Starting the return is not allowed in the current parcel state.",
  },
  "fulfillment.return.already_received": {
    fa: "دریافت این مرسوله در مبدأ قبلاً تأیید شده است.",
    en: "The origin receipt of this parcel is already confirmed.",
  },
  "fulfillment.return.receipt_invalid_status": {
    fa: "تأیید دریافت در مبدأ در وضعیت فعلی مرسوله مجاز نیست.",
    en: "The origin receipt is not allowed in the current parcel state.",
  },
  "fulfillment.return.receipt_already_recorded": {
    fa: "رسید دریافت این مرسوله قبلاً ثبت شده است.",
    en: "A receipt is already recorded for this parcel.",
  },
  "fulfillment.return.receipt_conflict": {
    fa: "رسید ثبت‌شده با اطلاعات ارسالی هم‌خوان نیست.",
    en: "The recorded receipt does not match the submitted lines.",
  },
  "fulfillment.resend.original_required": {
    fa: "مرسولهٔ مرجوعی برای ارسال مجدد مشخص نیست.",
    en: "The returned original parcel is missing.",
  },
  "fulfillment.resend.original_not_returned": {
    fa: "ارسال مجدد فقط پس از تأیید دریافت در مبدأ ممکن است.",
    en: "A resend requires a confirmed origin receipt.",
  },
  "fulfillment.resend.method_mismatch": {
    fa: "روش ارسال مجدد با روش پذیرفته‌شدهٔ اولیه هم‌خوان نیست.",
    en: "The resend method does not match the original accepted method.",
  },
  "fulfillment.resend.line_foreign": {
    fa: "این قلم در مرسولهٔ مرجوعی وجود ندارد.",
    en: "This line does not belong to the returned parcel.",
  },
  "fulfillment.resend.exceeds_returned": {
    fa: "تعداد ارسال مجدد از مقدار سالم دریافتی بیشتر است.",
    en: "The resend quantity exceeds the usable received quantity.",
  },
  "fulfillment.resend.concurrency_conflict": {
    fa: "ظرفیت برگشتی هم‌زمان تغییر کرد؛ فهرست را تازه‌سازی کنید و دوباره تلاش کنید.",
    en: "The returned capacity changed concurrently; refresh and try again.",
  },
  // TB-263: the damaged-goods decision family. Every code the owning aggregate can answer with must have a
  // concise operator sentence, so a known refusal is never presented as an unknown result.
  "fulfillment.damage.replacement_unavailable": {
    fa: "موجودی سالم این کالا برای جایگزینی کافی نیست؛ تصمیم ثبت شد و با رسیدن موجودی سالم همان تصمیم ادامه می‌یابد.",
    en: "No healthy stock for the replacement right now; the decision is recorded and continues when stock arrives.",
  },
  "fulfillment.damage.original_reservation_missing": {
    fa: "رزرو فروش اصلی این کالا قابل اثبات نیست؛ جایگزینی بدون آن انجام نمی‌شود.",
    en: "The original sale reservation cannot be proven; the replacement does not proceed without it.",
  },
  "fulfillment.damage.reservation_conflict": {
    fa: "رزرو دیگری به این جایگزینی متصل شده است؛ وضعیت مرسوله را تازه‌سازی کنید.",
    en: "Another reservation is already attached to this replacement; refresh the parcel state.",
  },
  "fulfillment.damage.replacement_conflict": {
    fa: "مرسولهٔ جایگزین دیگری به این تصمیم متصل شده است؛ وضعیت مرسوله را تازه‌سازی کنید.",
    en: "Another replacement parcel is already attached to this decision; refresh the parcel state.",
  },
  "fulfillment.damage.resolution_intent_conflict": {
    fa: "این درخواست با تصمیم ثبت‌شدهٔ کالای آسیب‌دیده یکسان نیست؛ همان تصمیم ثبت‌شده را ادامه دهید.",
    en: "This request differs from the recorded damaged-goods decision; continue that recorded decision.",
  },
  "fulfillment.damage.exceeds_outstanding": {
    fa: "مقدار واردشده از مقدار آسیب‌دیدهٔ تعیین‌تکلیف‌نشدهٔ همین مرسوله بیشتر است.",
    en: "The quantity exceeds this parcel line's outstanding damaged quantity.",
  },
  "fulfillment.damage.no_damaged_quantity": {
    fa: "این قلم مرسوله مقدار آسیب‌دیده ندارد.",
    en: "This parcel line carries no damaged quantity.",
  },
  "fulfillment.damage.obligation_already_settled": {
    fa: "این اقلام به مشتری تحویل شده‌اند؛ جایگزینی برای آن‌ها قابل تأمین نیست.",
    en: "These units were delivered to the customer; no replacement can be provided for them.",
  },
  "fulfillment.damage.source_not_returned": {
    fa: "این مرسوله هنوز در مبدأ دریافت فیزیکی نشده است.",
    en: "This parcel has not been physically received back at origin yet.",
  },
  "fulfillment.damage.receipt_missing": {
    fa: "برای این مرسوله رسید دریافت در مبدأ ثبت نشده است.",
    en: "No origin receipt is recorded for this parcel.",
  },
  "fulfillment.damage.line_foreign": {
    fa: "این قلم سفارش به مرسولهٔ انتخاب‌شده تعلق ندارد.",
    en: "This order line does not belong to the selected parcel.",
  },
  "fulfillment.damage.resolution_cancelled": {
    fa: "این تصمیم کالای آسیب‌دیده پیش از ارسال لغو شده است.",
    en: "This damaged-goods decision was cancelled before dispatch.",
  },
  "fulfillment.damage.resolution_not_open": {
    fa: "این تصمیم کالای آسیب‌دیده دیگر باز نیست؛ وضعیت مرسوله را تازه‌سازی کنید.",
    en: "This damaged-goods decision is no longer open; refresh the parcel state.",
  },
  "fulfillment.damage.resolution_not_replacement": {
    fa: "این تصمیم از نوع جایگزینی نیست.",
    en: "This decision is not a replacement.",
  },
  "fulfillment.damage.resolution_not_found": {
    fa: "تصمیم کالای آسیب‌دیده پیدا نشد؛ صفحه را تازه‌سازی کنید.",
    en: "The damaged-goods decision was not found; refresh the page.",
  },
  "fulfillment.damage.replacement_missing": {
    fa: "مرسولهٔ جایگزین متصل به این تصمیم پیدا نشد؛ صفحه را تازه‌سازی کنید.",
    en: "The replacement parcel linked to this decision was not found; refresh the page.",
  },
  "fulfillment.damage.reservation_required": {
    fa: "جایگزینی به رزرو اختصاصی موجودی سالم نیاز دارد.",
    en: "The replacement needs its own healthy-stock reservation.",
  },
  "fulfillment.damage.replacement_source_required": {
    fa: "جایگزینی به مرسولهٔ آسیب‌دیدهٔ مبدأ نیاز دارد.",
    en: "The replacement needs the damaged source parcel.",
  },
  "fulfillment.damage.replacement_occurrence_required": {
    fa: "جایگزینی به شناسهٔ درخواست بازبینی‌شده نیاز دارد.",
    en: "The replacement needs the reviewed request identity.",
  },
  "fulfillment.damage.resolution_occurrence_required": {
    fa: "تعیین تکلیف کالای آسیب‌دیده به شناسهٔ درخواست بازبینی‌شده نیاز دارد.",
    en: "The damaged-goods decision needs the reviewed request identity.",
  },
  "fulfillment.damage.resolution_ids_required": {
    fa: "شناسه‌های تصمیم کالای آسیب‌دیده کامل نیست.",
    en: "The damaged-goods decision identifiers are incomplete.",
  },
  "fulfillment.damage.resolution_qty_positive": {
    fa: "مقدار آسیب‌دیده باید بزرگ‌تر از صفر باشد.",
    en: "The damaged quantity must be greater than zero.",
  },
  "fulfillment.damage.resolution_outcome_invalid": {
    fa: "نوع تصمیم انتخاب‌شده برای کالای آسیب‌دیده معتبر نیست.",
    en: "The selected damaged-goods outcome is not valid.",
  },
  "fulfillment.damage.resolution_actor_required": {
    fa: "ثبت تصمیم کالای آسیب‌دیده به اپراتور بازبین نیاز دارد.",
    en: "Recording a damaged-goods decision needs the reviewing operator.",
  },
  "fulfillment.damage.resolution_note_invalid": {
    fa: "یادداشت اپراتور بیش از حد مجاز طولانی است.",
    en: "The operator note is too long.",
  },
  "fulfillment.damage.concurrency_conflict": {
    fa: "اپراتور دیگری این اقلام آسیب‌دیده را تعیین تکلیف کرد؛ صفحه را تازه کنید و دوباره بررسی کنید.",
    en: "Another operator resolved these damaged units; refresh and review again.",
  },
  "fulfillment.allocation.conflict": {
    fa: "تعداد از باقیماندهٔ قابل تخصیص به مرسوله بیشتر است.",
    en: "The quantity exceeds remaining allocatable packed amount.",
  },
  "inventory.reservation.not_active": {
    fa: "رزرو موجودی این سفارش دیگر فعال نیست. اطلاعات سفارش را تازه‌سازی کنید یا وضعیت رزرو را بررسی کنید.",
    en: "This order's inventory reservation is no longer active. Refresh the order or review reservation status.",
  },
  "inventory.manual_review.unavailable": {
    fa: "موجودی این سفارش در زمان بررسی پرداخت دیگر در دسترس نیست. لطفاً وضعیت سفارش و بازگشت وجه را بررسی کنید.",
    en: "Inventory for this order is no longer available during payment review. Check order status and refund handling.",
  },
  "payment.unpaid.supply_unavailable": {
    fa: "این سفارش در حال حاضر قابل تأمین نیست.",
    en: "This order cannot currently be supplied.",
  },
  "hold.online.invalid": {
    fa: "مقدار مهلت معتبر نیست.",
    en: "The hold duration is not valid.",
  },
  "inventory.supply.unavailable": {
    fa: "این سفارش در حال حاضر قابل تأمین نیست.",
    en: "This order cannot currently be supplied.",
  },
  "inventory.recovery.insufficient": {
    fa: "موجودی این سفارش پس از ثبت پرداخت مشتری در دسترس نیست. سفارش نیازمند تعیین تکلیف موجودی یا بازگشت وجه است.",
    en: "Inventory is unavailable after the customer payment was recorded. Resolve stock or refund the order.",
  },
  "inventory.recovery.required": {
    fa: "رزرو موجودی این سفارش از چرخه قبلی معتبر نیست و نیاز به بازیابی موجودی دارد.",
    en: "This order's inventory reservation from the prior lifecycle is invalid and requires recovery.",
  },
  "inventory.reservation.not_found": {
    fa: "رزرو موجودی این سفارش پیدا نشد. اطلاعات سفارش را تازه‌سازی کنید.",
    en: "This order's inventory reservation was not found. Refresh the order.",
  },
  "inventory.reservation.stock_mismatch": {
    fa: "مصرف رزرو با موجودی هم‌خوان نبود.",
    en: "Reservation consumption did not match stock quantities.",
  },
  "inventory.reservation.release_mismatch": {
    fa: "آزادسازی رزرو با موجودی هم‌خوان نبود؛ وضعیت رزرو این سفارش را بررسی کنید.",
    en: "The reservation release did not match stock quantities; review this order's reservation.",
  },
  "inventory.held_release.intent_conflict": {
    fa: "این درخواست آزادسازی با درخواست ثبت‌شدهٔ قبلی هم‌خوان نیست (رزرو یا مقدار متفاوت است). صفحه را تازه کنید و دوباره بررسی کنید.",
    en: "This release request does not match the recorded one (different reservation or quantity). Refresh and review again.",
  },
  "inventory.return_restock.invalid": {
    fa: "درخواست بازگشت کالا به موجودی معتبر نیست؛ مقدار و وضعیت رزرو را بررسی کنید.",
    en: "The return-restock request is not valid; check the quantity and reservation state.",
  },
  "order.operational_group.consolidate_not_marketplace": {
    fa: "تجمیع فقط وقتی ارسال بر عهدهٔ مارکت‌پلیس است مجاز است.",
    en: "Consolidation is allowed only when Marketplace owns the delivery responsibility.",
  },
  "order.operational_group.consolidate_incompatible": {
    fa: "گروه‌های انتخاب‌شده برای تجمیع سازگار نیستند (مبدأ، روش، روز یا ساعت متفاوت است).",
    en: "Selected groups are not compatible for consolidation (origin, method, day, or hour differ).",
  },
  "order.operational_group.consolidate_requires_multi_seller": {
    fa: "برای بستهٔ تجمیعی حداقل دو فروشنده لازم است.",
    en: "A consolidated package requires at least two sellers.",
  },
  "order.operational_group.consolidate_carrier_unsupported": {
    fa: "تجمیع محمولهٔ حامل API بین گروه‌ها پشتیبانی نمی‌شود؛ هر گروه را جداگانه ارسال کنید.",
    en: "Carrier API consolidation across groups is unsupported; dispatch each group separately.",
  },
  "fulfillment.shipment.locked_by_consolidated_package": {
    fa: "این مرسوله عضو بسته تجمیعی است و عملیات ارسال باید از طریق همان بسته انجام شود.",
    en: "This shipment belongs to a consolidated package; shipping actions must go through that package.",
  },
  "fulfillment.package.requires_multi_seller": {
    fa: "بسته تجمیعی حداقل به دو فروشندهٔ متمایز نیاز دارد.",
    en: "A consolidated package requires at least two distinct sellers.",
  },
  "fulfillment.package.shipment_not_eligible": {
    fa: "این مرسوله برای بسته تجمیعی واجد شرایط نیست.",
    en: "This shipment is not eligible for a consolidated package.",
  },
  "fulfillment.package.shipment_already_member": {
    fa: "این مرسوله هم‌اکنون عضو یک بسته تجمیعی فعال است.",
    en: "This shipment already belongs to an active consolidated package.",
  },
  "fulfillment.package.mixed_checkout": {
    fa: "فقط مرسوله‌های همین سفارش را می‌توان در یک بسته تجمیعی قرار داد.",
    en: "Only shipments from the same order can join one consolidated package.",
  },
  "fulfillment.package.duplicate_shipment": {
    fa: "مرسوله تکراری در بسته تجمیعی مجاز نیست.",
    en: "Duplicate shipments are not allowed in a consolidated package.",
  },
  "fulfillment.package.cancel_after_dispatch": {
    fa: "پس از ارسال بسته تجمیعی، ابطال مجاز نیست.",
    en: "A consolidated package cannot be cancelled after dispatch.",
  },
  "fulfillment.package.dispatch_invalid_state": {
    fa: "ارسال بسته تجمیعی در وضعیت فعلی مجاز نیست.",
    en: "Dispatch is not allowed for this consolidated package state.",
  },
  "fulfillment.package.deliver_before_dispatch": {
    fa: "قبل از ارسال نمی‌توان بسته تجمیعی را تحویل داد.",
    en: "A consolidated package cannot be delivered before dispatch.",
  },
  "fulfillment.package.member_state_changed": {
    fa: "وضعیت مرسوله‌های عضو تغییر کرده است؛ عملیات را تازه کنید.",
    en: "Member shipment state changed; refresh and try again.",
  },
  "fulfillment.package.not_found": {
    fa: "بسته تجمیعی پیدا نشد.",
    en: "The consolidated package was not found.",
  },
  "fulfillment.package.shipping_method_required": {
    fa: "روش ارسال مرسوله‌های عضو برای بسته تجمیعی الزامی است.",
    en: "Member shipments must have a shipping method for the consolidated package.",
  },
  "fulfillment.package.shipping_method_mismatch": {
    fa: "برای ایجاد بسته تجمیعی، روش ارسال مرسوله‌های انتخاب‌شده باید یکسان باشد.",
    en: "Selected shipments must share the same shipping method to create a consolidated package.",
  },
  "fulfillment.package.tracking_required": {
    fa: "برای ارسال بسته تجمیعی باید کد رهگیری مرکزی ثبت شود.",
    en: "A central tracking code is required before dispatching the consolidated package.",
  },
  "fulfillment.package.tracking_locked": {
    fa: "پس از ارسال بسته تجمیعی، تغییر کد رهگیری مجاز نیست.",
    en: "Consolidated package tracking cannot be changed after dispatch.",
  },
  "fulfillment.package.checkout_required": {
    fa: "شناسه سفارش برای بسته تجمیعی الزامی است.",
    en: "A checkout id is required for the consolidated package.",
  },
  "return.expired": {
    fa: "مهلت مرجوعی تمام شده است.",
    en: "The return window has expired.",
  },
  "return.non_returnable": {
    fa: "این کالا طبق سیاست سفارش قابل مرجوعی نیست.",
    en: "This item is not returnable under the order policy.",
  },
  "return.quantity_exceeded": {
    fa: "تعداد مرجوعی از باقیماندهٔ قابل مرجوعی بیشتر است.",
    en: "The return quantity exceeds the remaining returnable quantity.",
  },
  "return.quantity_invalid": {
    fa: "تعداد مرجوعی باید مثبت باشد.",
    en: "Return quantity must be positive.",
  },
  "return.not_delivered": {
    fa: "هنوز تحویلی ثبت نشده است.",
    en: "No delivery has been recorded yet.",
  },
  "return.not_paid": {
    fa: "مرجوعی فقط برای سفارش Paid مجاز است.",
    en: "Returns are allowed only for paid orders.",
  },
  "return.missing": {
    fa: "درخواست مرجوعی پیدا نشد.",
    en: "The return request was not found.",
  },
  "return.fulfillment_missing": {
    fa: "اطلاعات ارسال برای مرجوعی پیدا نشد.",
    en: "Fulfillment data for this return was not found.",
  },
  "return.stale": {
    fa: "وضعیت مرجوعی تغییر کرده است. صفحه را تازه کنید.",
    en: "The return state changed. Refresh the page.",
  },
  "return.already_approved": {
    fa: "این مرجوعی قبلاً تأیید شده است.",
    en: "This return has already been approved.",
  },
  "return.already_rejected": {
    fa: "این مرجوعی قبلاً رد شده است.",
    en: "This return has already been rejected.",
  },
  "return.rejected": {
    fa: "درخواست مرجوعی انجام نشد.",
    en: "The return request could not be completed.",
  },
  "refund.retry.invalid_state": {
    fa: "تلاش مجدد بازگشت وجه فقط برای وضعیت ناموفق مجاز است.",
    en: "Refund retry is allowed only after a failed refund.",
  },
  "refund.already_started": {
    fa: "بازگشت وجه قبلاً آغاز شده است.",
    en: "The refund has already started.",
  },
  "refund.already_completed": {
    fa: "بازگشت وجه قبلاً تکمیل شده است.",
    en: "The refund has already completed.",
  },
  "refund.payment_missing": {
    fa: "پرداخت مرجع برای بازگشت وجه پیدا نشد.",
    en: "The source payment for this refund was not found.",
  },
  "refund.destination.invalid": {
    fa: "مقصد بازگشت وجه نامعتبر است.",
    en: "The refund destination is not valid.",
  },
  "order.note.invalid": {
    fa: "متن یادداشت معتبر نیست.",
    en: "The note text is not valid.",
  },
  "order.invoice.unavailable": {
    fa: "فاکتور در دسترس نیست.",
    en: "Invoice is unavailable.",
  },
  "order.receipt.unavailable": {
    fa: "رسید پرداخت در دسترس نیست.",
    en: "Payment receipt is unavailable.",
  },
  "order.history.failed": {
    fa: "بارگذاری تاریخچه عملیات انجام نشد.",
    en: "Operational history could not be loaded.",
  },
  "host-unreachable": {
    fa: "اتصال به سرویس برقرار نیست. لطفاً دوباره تلاش کنید.",
    en: "Could not reach the service. Please try again.",
  },
  "seller.authorization.denied": {
    fa: "دسترسی مجاز نیست.",
    en: "You are not allowed to perform this action.",
  },
  "seller.identity.missing": {
    fa: "شناسه فروشنده مشخص نیست.",
    en: "Seller identity is missing.",
  },
  "media.type.unsupported": {
    fa: "نوع فایل رسانه پشتیبانی نمی‌شود. فقط تصویر JPEG، PNG، WebP یا GIF مجاز است.",
    en: "This media file type is not supported. Only JPEG, PNG, WebP, or GIF images are allowed.",
  },
  "media.too_large": {
    fa: "حجم فایل از سقف مجاز بیشتر است.",
    en: "The file exceeds the allowed size limit.",
  },
  "media.upload.failed": {
    fa: "بارگذاری رسانه ناموفق بود. لطفاً دوباره تلاش کنید.",
    en: "Media upload failed. Please try again.",
  },
  "media.missing": {
    fa: "رسانه یافت نشد.",
    en: "Media was not found.",
  },
  "media.storage.unavailable": {
    fa: "ذخیره‌سازی رسانه در دسترس نیست. لطفاً بعداً دوباره تلاش کنید.",
    en: "Media storage is unavailable. Please try again later.",
  },
  "content.article.locale_locked": {
    fa: "زبان این مقاله به‌دلیل وجود محتوا یا وابستگی‌های ثبت‌شده قابل تغییر نیست.",
    en: "This article's language cannot be changed because content or dependencies are already set.",
  },
  "content.article.create_failed": {
    fa: "ایجاد پیش‌نویس مقاله ناموفق بود.",
    en: "Could not create the article draft.",
  },
  "content.author.not_found": {
    fa: "نویسنده یافت نشد.",
    en: "Author was not found.",
  },
  "content.author.inactive": {
    fa: "نویسندهٔ غیرفعال برای انتساب جدید مجاز نیست.",
    en: "An inactive author cannot be newly assigned.",
  },
  "content.author.required_for_publish": {
    fa: "برای انتشار مقاله باید یک نویسنده انتخاب شود.",
    en: "An author is required before publishing the article.",
  },
  "content.publish.not_ready": {
    fa: "مقاله هنوز برای انتشار آماده نیست. موارد الزامی را تکمیل کنید.",
    en: "The article is not ready to publish. Complete the required items.",
  },
  "content.publish.invalid_schedule": {
    fa: "زمان انتشار معتبر نیست.",
    en: "The publish schedule is invalid.",
  },
  "content.publish.forbidden": {
    fa: "انتشار برای این وضعیت مجاز نیست.",
    en: "Publishing is not allowed for this article state.",
  },
  "content.unpublish.invalid_state": {
    fa: "لغو انتشار فقط برای مقالهٔ منتشرشده مجاز است.",
    en: "Unpublish is only allowed for a published article.",
  },
  "content.preview.unavailable": {
    fa: "پیش‌نمایش در دسترس نیست.",
    en: "Preview is unavailable.",
  },
  "content.article.missing": {
    fa: "مقاله یافت نشد.",
    en: "Article was not found.",
  },
  "content.comment.not_found": {
    fa: "نظر یافت نشد.",
    en: "Comment was not found.",
  },
  "content.comment.article_not_found": {
    fa: "مقالهٔ این نظر در دسترس نیست.",
    en: "The article for this comment is unavailable.",
  },
  "content.comment.invalid_transition": {
    fa: "این تغییر وضعیت برای نظر مجاز نیست.",
    en: "That comment status change is not allowed.",
  },
  "content.comment.invalid_payload": {
    fa: "اطلاعات نظر معتبر نیست.",
    en: "The comment details are not valid.",
  },
  "content.comment.forbidden": {
    fa: "اجازهٔ تعدیل این نظر را ندارید.",
    en: "You are not allowed to moderate this comment.",
  },
  "content.comment.rejected": {
    fa: "درخواست تعدیل نظر انجام نشد.",
    en: "The comment moderation request could not be completed.",
  },
  "content.help.unavailable": {
    fa: "راهنما در دسترس نیست.",
    en: "Help content is temporarily unavailable.",
  },
  "content.article.already_archived": {
    fa: "مقاله قبلاً بایگانی شده است.",
    en: "The article is already archived.",
  },
  "content.article.archive_not_allowed": {
    fa: "بایگانی برای این مقاله مجاز نیست.",
    en: "Archive is not allowed for this article.",
  },
  "localization.language.inactive": {
    fa: "زبان انتخاب‌شده فعال نیست.",
    en: "The selected language is not active.",
  },
  "content.update.rejected": {
    fa: "ذخیرهٔ مقاله انجام نشد. لطفاً ورودی‌ها را بررسی کنید.",
    en: "The article could not be saved. Please review the fields and try again.",
  },
  "content.category.language_mismatch": {
    fa: "زبان دسته‌بندی باید با زبان مقاله یکسان باشد.",
    en: "The category language must match the article language.",
  },
  "content.category.not_found": {
    fa: "دستهٔ مقاله یافت نشد.",
    en: "The article category was not found.",
  },
  "content.category.invalid_language": {
    fa: "زبان دسته نامعتبر است.",
    en: "The category language is not valid.",
  },
  "content.category.max_depth_exceeded": {
    fa: "دسته‌بندی مقاله حداکثر می‌تواند دو سطح داشته باشد.",
    en: "Article categories can have at most two levels.",
  },
  "content.category.invalid_parent": {
    fa: "والد انتخاب‌شده برای دسته معتبر نیست.",
    en: "The selected parent category is not valid.",
  },
  "content.category.inactive": {
    fa: "دستهٔ بایگانی‌شده برای انتساب جدید مجاز نیست.",
    en: "An archived category cannot be newly assigned.",
  },
  "content.category.cross_language_parent": {
    fa: "والد باید همان زبان دسته باشد.",
    en: "The parent category must use the same language.",
  },
  "content.category.slug_duplicate": {
    fa: "این نامک برای یک دستهٔ دیگر در همین زبان استفاده شده است.",
    en: "This slug is already used by another category in the same language.",
  },
  "content.tag.duplicate_name": {
    fa: "این برچسب قبلاً ثبت شده است.",
    en: "This tag already exists.",
  },
  "content.tag.language_mismatch": {
    fa: "زبان برچسب باید با زبان مقاله یکسان باشد.",
    en: "The tag language must match the article language.",
  },
  "content.tag.inactive": {
    fa: "برچسب غیرفعال برای انتساب جدید مجاز نیست.",
    en: "An inactive tag cannot be newly assigned.",
  },
  "content.tag.not_found": {
    fa: "برچسب یافت نشد.",
    en: "The tag was not found.",
  },
  "content.tag.invalid_name": {
    fa: "نام برچسب معتبر نیست.",
    en: "The tag name is not valid.",
  },
  "content.tag.invalid_language": {
    fa: "زبان برچسب نامعتبر است.",
    en: "The tag language is not valid.",
  },
  "content.tag.article_not_found": {
    fa: "مقاله برای انتساب برچسب یافت نشد.",
    en: "The article for tag assignment was not found.",
  },
  "content.article.media_not_found": {
    fa: "رسانهٔ انتخاب‌شده در دسترس نیست یا یافت نشد.",
    en: "The selected media asset is unavailable or was not found.",
  },
  "content.article.media.rejected": {
    fa: "اختصاص رسانه به مقاله ناموفق بود.",
    en: "Could not assign media to the article.",
  },
  "content.article.unsafe_body_media": {
    fa: "بدنه شامل رسانهٔ ناامن است. فقط تصاویر کتابخانه مجازند.",
    en: "The body contains unsafe media. Only library images are allowed.",
  },
  "landing.slug.invalid": {
    fa: "آدرس صفحه معتبر نیست. فقط حروف، عدد و خط تیره استفاده کنید.",
    en: "The page address is not valid.",
  },
  "landing.slug.reserved": {
    fa: "این آدرس برای مسیرهای سامانه رزرو شده است. آدرس دیگری انتخاب کنید.",
    en: "This address is reserved. Choose another page address.",
  },
  "landing.slug.duplicate": {
    fa: "این آدرس در همین زبان قبلاً ثبت شده است.",
    en: "This page address is already used in this language.",
  },
  "landing.title.required": {
    fa: "عنوان صفحه لازم است.",
    en: "A page title is required.",
  },
  "landing.status.invalid": {
    fa: "وضعیت انتشار معتبر نیست.",
    en: "The publication status is not valid.",
  },
  "landing.home.ineligible": {
    fa: "فقط صفحهٔ منتشرشده را می‌توان خانه کرد.",
    en: "Only a published page can be selected as Home.",
  },
  "landing.page.missing": {
    fa: "صفحه یافت نشد.",
    en: "The page was not found.",
  },
  "landing.section.limit": {
    fa: "تعداد بخش‌های صفحه به سقف رسیده است.",
    en: "This page has reached the section limit.",
  },
  "landing.section.type.invalid": {
    fa: "این نوع بخش پشتیبانی نمی‌شود.",
    en: "This section type is not supported.",
  },
  "landing.section.config.invalid": {
    fa: "تنظیمات بخش معتبر نیست.",
    en: "The section settings are not valid.",
  },
  "landing.section.config.forbidden": {
    fa: "تنظیم اجرایی یا پرس‌وجوی آزاد مجاز نیست.",
    en: "Executable or free-form query settings are not allowed.",
  },
  "landing.section.source.unsupported": {
    fa: "این منبع هنوز پشتیبانی نمی‌شود.",
    en: "This source is not supported yet.",
  },
  "landing.section.source.invalid": {
    fa: "منبع انتخاب‌شده معتبر نیست.",
    en: "The selected source is not valid.",
  },
  "landing.section.source.missing": {
    fa: "منبع بخش را کامل کنید.",
    en: "Complete the section source.",
  },
  "landing.section.ref.missing": {
    fa: "مورد انتخاب‌شده در این فروشگاه نیست.",
    en: "The selected item is not in this store.",
  },
  "landing.section.title.required": {
    fa: "عنوان بخش لازم است.",
    en: "A section title is required.",
  },
  "landing.section.text.required": {
    fa: "متن بخش لازم است.",
    en: "Section text is required.",
  },
  "landing.section.text.html": {
    fa: "HTML در متن مجاز نیست.",
    en: "HTML is not allowed in this text.",
  },
  "landing.section.take.invalid": {
    fa: "تعداد نمایش باید بین ۱ تا ۲۴ باشد.",
    en: "Display count must be between 1 and 24.",
  },
  "landing.section.reorder.invalid": {
    fa: "ترتیب بخش‌ها کامل نیست.",
    en: "The section order is incomplete.",
  },
  "landing.section.missing": {
    fa: "بخش یافت نشد.",
    en: "The section was not found.",
  },
  "menu.title.required": { fa: "عنوان منو لازم است.", en: "Menu title is required." },
  "menu.key.duplicate": { fa: "کلید منو تکراری است.", en: "Menu key already exists." },
  "menu.missing": { fa: "منو یافت نشد.", en: "The menu was not found." },
  "menu.delete.referenced": { fa: "این منو در حال استفاده است و حذف نمی‌شود.", en: "This menu is in use and cannot be deleted." },
  "menu.item.missing": { fa: "آیتم منو یافت نشد.", en: "The menu item was not found." },
  "menu.item.label.required": { fa: "عنوان آیتم لازم است.", en: "Menu item label is required." },
  "menu.item.cycle": { fa: "چرخه در درخت منو مجاز نیست.", en: "Menu cycles are not allowed." },
  "menu.item.depth": { fa: "حداکثر سه سطح تو در تو مجاز است.", en: "Menus are limited to three levels." },
  "menu.item.parent.invalid": { fa: "والد باید در همین منو باشد.", en: "The parent must belong to the same menu." },
  "menu.item.reorder.invalid": { fa: "ترتیب آیتم‌ها کامل نیست.", en: "The menu order is incomplete." },
  "menu.link.invalid": { fa: "نوع مقصد منو معتبر نیست.", en: "The destination type is not valid." },
  "menu.url.required": { fa: "نشانی بیرونی لازم است.", en: "An external address is required." },
  "menu.url.unsafe": { fa: "فقط نشانی وب امن مجاز است.", en: "Only safe web addresses are allowed." },
  "menu.target.required": { fa: "مقصد را از فهرست انتخاب کنید.", en: "Choose a destination from the list." },
  "menu.target.missing": { fa: "مقصد انتخاب‌شده در این فروشگاه نیست.", en: "The selected destination is not in this store." },
  "menu.header.ineligible": { fa: "فقط منوی فعال قابل انتخاب است.", en: "Only an enabled menu can be selected." },
  "returns.courier_origin.source_required": {
    fa: "برای پروندهٔ مرجوعی مبدأ پیک، مرسوله و رسید دریافت مبدأ لازم است.",
    en: "A courier-origin return requires the source shipment and its receipt.",
  },
  "returns.courier_origin.claim_required": {
    fa: "ادعای دریافت مبدأ پیدا نشد؛ رسید و خطوط ادعا را بررسی کنید.",
    en: "The source claim was not found; check the receipt and claim lines.",
  },
  "returns.courier_origin.occurrence_required": {
    fa: "شناسهٔ رویداد این مرجوعی لازم است.",
    en: "The occurrence key of this return is required.",
  },
  "returns.validation.items_empty": {
    fa: "حداقل یک قلم برای مرجوعی لازم است.",
    en: "At least one item is required.",
  },
  "returns.validation.quantity_invalid": {
    fa: "مقدار مرجوعی معتبر نیست.",
    en: "The returned quantity is not valid.",
  },
  "returns.refund.retry": {
    fa: "بازگشت وجه ناتمام مانده است؛ تلاش مجدد بازگشت وجه را تکرار کنید.",
    en: "The refund is unfinished; retry the refund.",
  },
  "returns.refund.failed": {
    fa: "بازگشت وجه ناموفق بود؛ پرونده برای تلاش مجدد باز می‌ماند.",
    en: "The refund failed; the case stays open for retry.",
  },
  "fulfillment.returns.replacement_claimed_by_case": {
    fa: "این مرسولهٔ جایگزین به یک پروندهٔ مرجوعی باز درخواست‌شده تعلق دارد و تا تعیین تکلیف پرونده ارسال نمی‌شود.",
    en: "This replacement parcel belongs to an open requested return case and cannot be dispatched until the case is resolved.",
  },
  "fulfillment.returns.replacement_already_dispatched": {
    fa: "مرسولهٔ جایگزین قبلاً ارسال شده است.",
    en: "The replacement parcel has already been dispatched.",
  },
  "fulfillment.returns.source_not_received": {
    fa: "مرسولهٔ مبدأ هنوز در مبدأ دریافت نشده است.",
    en: "The source shipment has not been received at origin yet.",
  },
  "fulfillment.returns.claim_quantity_exceeded": {
    fa: "مقدار ادعا از مقدار سالم قابل مرجوع بیشتر است.",
    en: "The claimed quantity exceeds the usable quantity available for return.",
  },
  "fulfillment.returns.claim_condition_invalid": {
    fa: "وضعیت کالای ادعاشده معتبر نیست.",
    en: "The claimed item condition is not valid.",
  },
  "fulfillment.returns.claim_already_approved": {
    fa: "این ادعا قبلاً تأیید شده است.",
    en: "This claim has already been approved.",
  },
  "fulfillment.returns.claim_case_conflict": {
    fa: "این ادعا به پروندهٔ مرجوعی دیگری متصل است.",
    en: "This claim is linked to a different return case.",
  },
  "fulfillment.returns.claim_concurrency_conflict": {
    fa: "هم‌زمانی در ثبت ادعا رخ داد؛ دوباره تلاش کنید.",
    en: "A concurrency conflict occurred while recording the claim; try again.",
  },
};

const TECHNICAL_UI_PATTERNS = [
  /\bbad\s*request\b/i,
  /\binternal\s*server\s*error\b/i,
  /\bnot\s*found\b/i,
  /\bforbidden\b/i,
  /\bunauthorized\b/i,
  /\bHTTP\s*[0-9]{3}\b/i,
  /\bstatus\s*[:=]?\s*[0-9]{3}\b/i,
  /\bException\b/,
  /\bStackTrace\b/i,
  /\bat\s+\w+\.\w+/,
  /^\s*\{[\s\S]*\}\s*$/,
];

/** آیا متن برای نمایش عادی UI خطرناک/فنی است؟ */
export function isTechnicalAdminErrorText(raw: string): boolean {
  const t = raw.trim();
  if (!t) return true;
  if (/^admin\.http\.\d+$/i.test(t)) return true;
  if (/^host-grid-http-\d+$/i.test(t)) return true;
  if (/^host-.*-http-\d+$/i.test(t)) return true;
  return TECHNICAL_UI_PATTERNS.some((re) => re.test(t));
}

/**
 * استخراج کد پایدار از پیام/بدنه — اگر کد شناخته‌شده در متن باشد همان را برمی‌گرداند.
 */
export function extractAdminErrorCode(raw: string | null | undefined): string | null {
  if (!raw) return null;
  const trimmed = raw.trim();
  if (!trimmed) return null;
  if (ADMIN_ERROR_MESSAGES[trimmed]) return trimmed;
  const paren = trimmed.match(/\(([a-z0-9]+(?:\.[a-z0-9_-]+)+)\)\s*$/i);
  if (paren?.[1] && ADMIN_ERROR_MESSAGES[paren[1]]) return paren[1];
  const dotted = trimmed.match(/\b([a-z][a-z0-9_-]*(?:\.[a-z0-9_-]+)+)\b/i);
  if (dotted?.[1] && ADMIN_ERROR_MESSAGES[dotted[1]]) return dotted[1];
  if (trimmed === "host-unreachable" || trimmed.startsWith("workspace.host.")) {
    return "host-unreachable";
  }
  return null;
}

/**
 * نگاشت کد/پیام خام به متن کاربرپسند برای locale فعلی.
 * کد خام، Bad Request، HTTP status و stack هرگز برنمی‌گردند.
 */
export function mapAdminErrorMessage(
  raw: string | null | undefined,
  locale: AdminErrorLocale = "fa",
): string {
  const code = extractAdminErrorCode(raw);
  if (code && ADMIN_ERROR_MESSAGES[code]) {
    return ADMIN_ERROR_MESSAGES[code]![locale];
  }
  const text = (raw ?? "").trim();
  if (text && !isTechnicalAdminErrorText(text) && !/^[a-z0-9]+(?:\.[a-z0-9_-]+)+$/i.test(text)) {
    return text;
  }
  return locale === "en" ? UNKNOWN_EN : UNKNOWN_FA;
}

/** پیام ناشناختهٔ استاندارد. */
export function unknownAdminErrorMessage(locale: AdminErrorLocale = "fa"): string {
  return locale === "en" ? UNKNOWN_EN : UNKNOWN_FA;
}

/** فهرست کدهای شناخته‌شده (برای تست). */
export function listMappedAdminErrorCodes(): string[] {
  return Object.keys(ADMIN_ERROR_MESSAGES);
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function textProp(item: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = item[key];
    if (typeof value === "string" && value.trim()) return value.trim();
  }
  return "";
}

/**
 * مسیر واحد نرمال‌سازی خطای Admin از payload پاسخ Host / fetch.
 * فقط کد پایدار یا نشانگر HTTP داخلی؛ هرگز title خام مثل Bad Request.
 */
export function parseAdminProblemErrorCode(payload: unknown, status: number): string {
  if (status === 401 || status === 403) return "admin.authorization.denied";
  const item = recordOf(payload);
  if (item) {
    const code = textProp(item, "errorCode", "ErrorCode", "code", "Code");
    const detail = textProp(item, "detail", "Detail", "message", "Message");
    // وقتی Host کد عمومی برمی‌گرداند، detail اغلب کد دامنهٔ واقعی است.
    if (code === "content.update.rejected" || code === "content.create.rejected" || code === "content.article.media.rejected") {
      const fromDetail = extractAdminErrorCode(detail);
      if (fromDetail && fromDetail !== code) return fromDetail;
    }
    if (code) return code;
    const fromDetailOnly = extractAdminErrorCode(detail);
    if (fromDetailOnly) return fromDetailOnly;
    const extensions = recordOf(item.extensions ?? item.Extensions);
    if (extensions) {
      const nested = textProp(extensions, "errorCode", "ErrorCode", "code", "Code");
      if (nested) return nested;
    }
  }
  if (status <= 0) return "host-unreachable";
  return `admin.http.${status}`;
}

/**
 * نرمال‌سازی کامل برای toast/فرم: کد پایدار → پیام انسان‌خوان locale.
 */
export function normalizeAdminClientError(
  payload: unknown,
  status: number,
  locale: AdminErrorLocale = "fa",
): string {
  return mapAdminErrorMessage(parseAdminProblemErrorCode(payload, status), locale);
}
