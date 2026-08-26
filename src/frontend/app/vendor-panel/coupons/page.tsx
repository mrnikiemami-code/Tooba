import { Tag } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorcouponsPage() {
  return (
    <VendorCapabilityShell
      title="تخفیف‌ها"
      description="کوپن و پروموشن فروشنده"
      icon={<Tag className="w-5 h-5" />}
    />
  );
}
