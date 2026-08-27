"use client";

import { NotificationInbox } from "../notification-inbox";

/** اینباکس اعلان مشتری — Host واقعی + قفل UI شاپیوا. */
export default function CustomerNotificationsPage() {
  return (
    <NotificationInbox
      kind="customer"
      emptyHint="پس از رویدادهای واقعی پرداخت، ارسال یا مرجوعی، اعلان اینجا ظاهر می‌شود."
    />
  );
}
