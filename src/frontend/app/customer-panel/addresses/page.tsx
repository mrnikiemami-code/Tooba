import { MapPin } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** پوستهٔ آدرس‌ها؛ Order shipping snapshot دفترچهٔ آدرس قابل‌ویرایش نیست. */
export default function CustomerAddressesPage() {
  return (
    <CustomerCapabilityShell
      title="آدرس‌های من"
      description="مدیریت نشانی‌های تحویل"
      icon={<MapPin className="w-5 h-5" />}
    />
  );
}
