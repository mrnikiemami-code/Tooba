import { Ticket } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** تیکت پشتیبانی — capability معتبر در Backend نیست؛ پوستهٔ صادقانه. */
export default function CustomerTicketsPage() {
  return (
    <CustomerCapabilityShell
      title="تیکت‌ها"
      description="پشتیبانی و پیگیری درخواست‌های مشتری"
      icon={<Ticket className="w-5 h-5" />}
    />
  );
}
