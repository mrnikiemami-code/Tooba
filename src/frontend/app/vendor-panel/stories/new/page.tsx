"use client";

import { SELLER_STORY_CAPABILITIES } from "../../../stories/management";
import { StoryManagementRoute } from "../../../stories/management/StoryManagementRoute";

/** مسیر پایدار ساخت استوری فروشنده داخل VendorShell. */
export default function VendorStoriesNewPage() {
  return <StoryManagementRoute capabilities={SELLER_STORY_CAPABILITIES} createMode />;
}
