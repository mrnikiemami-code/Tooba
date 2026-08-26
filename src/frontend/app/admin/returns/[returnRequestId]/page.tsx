import { AdminReturnDetailScreen } from "../../admin-screens";

/** جزئیات مرجوعی Admin. */
export default async function AdminReturnDetailPage({ params }: { params: Promise<{ returnRequestId: string }> }) {
  const { returnRequestId } = await params;
  return <AdminReturnDetailScreen returnRequestId={returnRequestId} />;
}
