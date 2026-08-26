import { Wallet } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorwalletPage() {
  return (
    <VendorCapabilityShell
      title="کیف پول"
      description="موجودی و تسویه فروشنده"
      icon={<Wallet className="w-5 h-5" />}
    />
  );
}
