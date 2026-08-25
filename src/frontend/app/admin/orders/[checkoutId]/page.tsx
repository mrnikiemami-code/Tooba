import { AdminOrderDetailScreen } from "../../admin-screens";

/** مسیر جزئیات checkout برای Admin. */
export default async function AdminOrderDetailPage({ params }: { params: Promise<{ checkoutId: string }> }) {
  const { checkoutId } = await params;
  return <AdminOrderDetailScreen checkoutId={checkoutId} />;
}
