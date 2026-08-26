import { Ticket } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorticketsPage() {
  return (
    <VendorCapabilityShell
      title="تیکت‌ها"
      description="پشتیبانی فروشنده"
      icon={<Ticket className="w-5 h-5" />}
    />
  );
}
