import { Settings } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorsettingsPage() {
  return (
    <VendorCapabilityShell
      title="تنظیمات"
      description="اطلاعات فروشگاه و پروفایل فروشنده"
      icon={<Settings className="w-5 h-5" />}
    />
  );
}
