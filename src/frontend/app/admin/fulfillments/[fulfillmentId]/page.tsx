import { AdminFulfillmentDetailScreen } from "../../admin-screens";

/** مسیر جزئیات fulfillment Admin. */
export default async function AdminFulfillmentDetailPage({ params }: { params: Promise<{ fulfillmentId: string }> }) {
  const { fulfillmentId } = await params;
  return <AdminFulfillmentDetailScreen fulfillmentId={fulfillmentId} />;
}
