import { AdminCampaignWorkspace } from "../admin-campaign-workspace.tsx";

/** فضای کار ویرایش کمپین فروش. */
export default async function AdminCampaignEditPage({
  params,
}: {
  params: Promise<{ campaignId: string }>;
}) {
  const { campaignId } = await params;
  return <AdminCampaignWorkspace campaignId={campaignId} />;
}
