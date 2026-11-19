"use client";

import { useRouter } from "next/navigation";
import { StoryManagementScreen } from "./StoryManagementScreen";
import type { StoryCapabilities } from "./story-capabilities";

/** Thin Next router adapter around the shared Story workspace. */
export function StoryManagementRoute({
  capabilities,
  initialStoryId = null,
  createMode = false,
}: {
  capabilities: StoryCapabilities;
  initialStoryId?: string | null;
  createMode?: boolean;
}) {
  const router = useRouter();
  return (
    <StoryManagementScreen
      capabilities={capabilities}
      initialStoryId={initialStoryId}
      createMode={createMode}
      navigate={(href) => router.push(href)}
    />
  );
}
