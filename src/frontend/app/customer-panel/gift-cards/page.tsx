import { CreditCard } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** پوستهٔ کارت هدیه تا زمان وجود ledger و redemption معتبر. */
export default function CustomerGiftCardsPage() {
  return (
    <CustomerCapabilityShell
      title="کارت‌های هدیه"
      description="کارت‌های فعال و استفاده‌شده"
      icon={<CreditCard className="w-5 h-5" />}
    />
  );
}
