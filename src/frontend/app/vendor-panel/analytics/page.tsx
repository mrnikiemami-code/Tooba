import { BarChart3 } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendoranalyticsPage() {
  return (
    <VendorCapabilityShell
      title="آمار و نمودار"
      description="گزارش عملکرد فروش"
      icon={<BarChart3 className="w-5 h-5" />}
    />
  );
}
