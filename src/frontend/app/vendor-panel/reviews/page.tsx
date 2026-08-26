import { Star } from "lucide-react";
import { VendorCapabilityShell } from "../vendor-capability-shell";

export default function VendorreviewsPage() {
  return (
    <VendorCapabilityShell
      title="نظرات"
      description="نظرات محصولات فروشنده"
      icon={<Star className="w-5 h-5" />}
    />
  );
}
