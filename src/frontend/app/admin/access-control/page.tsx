"use client";

import { useMemo } from "react";
import { AccessControlCenter } from "../../access-control/access-control-center";
import { createAdminAccessApi } from "../../access-control/access-control-api";

/** صفحهٔ مرکز کنترل دسترسی Admin. */
export default function AdminAccessControlPage() {
  const api = useMemo(() => createAdminAccessApi(), []);
  return <AccessControlCenter mode="admin" title="مرکز کنترل دسترسی" api={api} canManage />;
}
