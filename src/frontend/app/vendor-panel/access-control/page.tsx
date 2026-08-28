"use client";

import { useMemo } from "react";
import { Shield } from "lucide-react";
import { AccessControlCenter } from "../../access-control/access-control-center";
import { createSellerAccessApi } from "../../access-control/access-control-api";

/**
 * کنترل دسترسی فروشنده داخل کروم تنظیمات Vendor (gradient / card).
 */
export default function VendorAccessControlPage() {
  const api = useMemo(() => createSellerAccessApi(), []);
  return (
    <main className="space-y-6" data-testid="vendor-access-control-page">
      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden shadow-sm">
        <div className="p-4 md:p-6 border-b border-gray-200 bg-gradient-to-r from-[#2563EB]/5 to-transparent">
          <div className="flex items-center gap-2">
            <Shield className="w-5 h-5 text-[#2563EB]" />
            <h1 className="text-lg font-bold text-gray-900">کنترل دسترسی</h1>
          </div>
          <p className="text-sm text-gray-500 mt-1">نقش‌ها و مجوزهای تیم فروشگاه — زبان تنظیمات فروشنده</p>
        </div>
        <div className="p-4 md:p-6">
          <AccessControlCenter mode="seller" title="کنترل دسترسی فروشگاه" api={api} canManage />
        </div>
      </div>
    </main>
  );
}
