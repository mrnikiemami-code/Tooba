import { Heart } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** پوستهٔ علاقه‌مندی Shopeiva بدون اختراع storage سمت کاربر. */
export default function CustomerWishlistPage() {
  return (
    <CustomerCapabilityShell
      title="علاقه‌مندی‌های من"
      description="محصولات نشان‌شده برای خرید آینده"
      icon={<Heart className="w-5 h-5" />}
    />
  );
}
