"use client";

import { use } from "react";
import { CouponForm } from "../../coupon-form";

/** ویرایش کد تخفیف پیش‌نویس/منقضی. */
export default function VendorCouponEditPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  return <CouponForm promotionId={id} />;
}
