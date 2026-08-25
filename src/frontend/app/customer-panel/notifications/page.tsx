import { Bell } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** پوستهٔ اعلان‌های مشتری تا زمان وجود منبع اعلان معتبر. */
export default function CustomerNotificationsPage() {
  return (
    <CustomerCapabilityShell
      title="اعلان‌ها"
      description="پیام‌ها و رویدادهای حساب مشتری"
      icon={<Bell className="w-5 h-5" />}
    />
  );
}
