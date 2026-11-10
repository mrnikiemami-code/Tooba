import { AdminOrderOperationsRouteScreen } from "./admin-order-operations-route-screen";

/**
 * TB-260: the shared operational order view the work queue opens directly.
 * Only locators travel in the URL; the server re-authorises and re-projects everything.
 */
export default async function AdminOrderOperationsPage({
  params,
  searchParams,
}: {
  params: Promise<{ checkoutId: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const { checkoutId } = await params;
  const query = await searchParams;
  const pick = (key: string): string | null => {
    const raw = query[key];
    const value = Array.isArray(raw) ? raw[0] : raw;
    return value && value.trim().length > 0 ? value.trim() : null;
  };
  return (
    <AdminOrderOperationsRouteScreen
      checkoutId={checkoutId}
      fulfillmentId={pick("fulfillmentId")}
      shippingGroupId={pick("shippingGroupId")}
      shipmentId={pick("shipmentId")}
      packageId={pick("packageId")}
      fromQueue={pick("from") === "queue"}
      backFilter={pick("filter")}
    />
  );
}
