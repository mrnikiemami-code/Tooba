"use client";

import { useMemo } from "react";
import { useParams } from "next/navigation";
import { AccessControlCenter } from "../../../../access-control/access-control-center";
import { createAdminSellerAccessApi } from "../../../../access-control/access-control-api";

/** Access Control فروشنده از دید Admin. */
export default function AdminSellerAccessControlPage() {
  const params = useParams<{ sellerId: string }>();
  const sellerId = params.sellerId;
  const api = useMemo(() => createAdminSellerAccessApi(sellerId), [sellerId]);
  return (
    <AccessControlCenter
      mode="admin-seller"
      title="کنترل دسترسی فروشنده"
      api={api}
      canManage
    />
  );
}
