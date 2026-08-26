import { Gift } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorgiftcardsPage() {
  return (
    <VendorCapabilityShell
      title="کارت‌های هدیه"
      description="مدیریت کارت هدیه"
      icon={<Gift className="w-5 h-5" />}
    />
  );
}
