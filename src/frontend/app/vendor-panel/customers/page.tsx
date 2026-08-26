import { Users } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorcustomersPage() {
  return (
    <VendorCapabilityShell
      title="مشتریان"
      description="فهرست خریداران مرتبط با فروشنده"
      icon={<Users className="w-5 h-5" />}
    />
  );
}
