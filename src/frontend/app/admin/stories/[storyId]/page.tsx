import { AdminStoriesDetailScreen } from "../../admin-screens";

/** مسیر پایدار جزئیات استوری ادمین. */
export default async function AdminStoriesDetailPage({
  params,
}: {
  params: Promise<{ storyId: string }>;
}) {
  const { storyId } = await params;
  return <AdminStoriesDetailScreen storyId={storyId} />;
}
