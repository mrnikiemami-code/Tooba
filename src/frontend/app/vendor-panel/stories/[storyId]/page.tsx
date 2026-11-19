"use client";

import { use } from "react";
import { SELLER_STORY_CAPABILITIES } from "../../../stories/management";
import { StoryManagementRoute } from "../../../stories/management/StoryManagementRoute";

/** مسیر پایدار جزئیات استوری فروشنده داخل VendorShell. */
export default function VendorStoriesDetailPage({
  params,
}: {
  params: Promise<{ storyId: string }>;
}) {
  const { storyId } = use(params);
  return <StoryManagementRoute capabilities={SELLER_STORY_CAPABILITIES} initialStoryId={storyId} />;
}
