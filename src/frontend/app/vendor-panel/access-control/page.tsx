"use client";

import { useMemo } from "react";
import { AccessControlCenter } from "../../access-control/access-control-center";
import { createSellerAccessApi } from "../../access-control/access-control-api";

/** صفحهٔ مرکز کنترل دسترسی فروشنده. */
export default function VendorAccessControlPage() {
  const api = useMemo(() => createSellerAccessApi(), []);
  return <AccessControlCenter mode="seller" title="کنترل دسترسی فروشگاه" api={api} canManage />;
}
