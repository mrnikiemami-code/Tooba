import { Settings } from "lucide-react";
import { CustomerCapabilityShell } from "../customer-capability-shell";

/** تنظیمات حساب — capability امنیتی/اعلان معتبر در Backend نیست؛ پوستهٔ صادقانه. */
export default function CustomerSettingsPage() {
  return (
    <CustomerCapabilityShell
      title="تنظیمات"
      description="امنیت، اعلان‌ها و ترجیحات حساب"
      icon={<Settings className="w-5 h-5" />}
    />
  );
}
