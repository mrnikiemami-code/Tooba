import { WalletCards } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** پوستهٔ کیف پول؛ Payment سفارش معادل wallet ledger نیست. */
export default function CustomerWalletPage() {
  return (
    <CustomerCapabilityShell
      title="کیف پول"
      description="موجودی و گردش حساب مشتری"
      icon={<WalletCards className="w-5 h-5" />}
    />
  );
}
