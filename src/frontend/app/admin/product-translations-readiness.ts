export type TranslationReadiness = "complete" | "partial" | "missing";

export type TranslationDraftLike = {
  name: string;
  shortDescription: string;
  description: string;
  seoTitle?: string;
  seoDescription?: string;
};

function descriptionHasContent(description: string): boolean {
  const plain = String(description ?? "")
    .replace(/<[^>]+>/g, " ")
    .replace(/&nbsp;/g, " ")
    .trim();
  return plain.length > 0;
}

export function translationReadiness(draft: TranslationDraftLike): TranslationReadiness {
  const name = draft.name.trim();
  const short = draft.shortDescription.trim();
  const full = descriptionHasContent(draft.description);
  if (!name && !short && !full) return "missing";
  if (name && short && full) return "complete";
  return "partial";
}
