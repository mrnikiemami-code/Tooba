"use client";

import { NotificationInbox } from "../../customer-panel/notification-inbox";

/** اینباکس اعلان فروشنده — همان UI شاپیوا با API فروشنده. */
export default function VendorNotificationsPage() {
  return (
    <NotificationInbox
      kind="seller"
      emptyHint="پس از سفارش پرداخت‌شده، ارسال یا مرجوعی مرتبط با فروشگاه شما، اعلان اینجا ظاهر می‌شود."
    />
  );
}
