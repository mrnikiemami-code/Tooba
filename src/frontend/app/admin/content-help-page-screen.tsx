"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import {
  CONTENT_HELP_PAGE_HREF,
  CONTENT_HELP_TOPICS,
  contentHelpTitle,
  type ContentHelpLocale,
} from "./content-help-content.ts";

function resolveLocale(): ContentHelpLocale {
  if (typeof document !== "undefined" && document.documentElement.lang?.toLowerCase().startsWith("en")) {
    return "en";
  }
  return "fa";
}

/** صفحهٔ راهنمای مرکزی Admin Content. */
export function ContentHelpPageScreen() {
  const [locale] = useState<ContentHelpLocale>(() => resolveLocale());
  const topics = useMemo(() => CONTENT_HELP_TOPICS, []);
  const isEn = locale === "en";

  return (
    <main className="mx-auto w-full max-w-3xl space-y-6 p-4" data-testid="content-help-page" dir={isEn ? "ltr" : "rtl"}>
      <header className="space-y-2 rounded-2xl border bg-surface-elevated p-4">
        <Link href="/admin/content" className="text-sm text-[#2563EB] underline">
          {isEn ? "Back to Content" : "بازگشت به محتوا"}
        </Link>
        <h1 className="text-xl font-bold">{isEn ? "Content help" : "راهنمای محتوا"}</h1>
        <p className="text-sm text-muted">
          {isEn
            ? "Short explanations for common Content concepts. No technical jargon."
            : "توضیح کوتاه مفاهیم رایج محتوا به زبان ساده."}
        </p>
        <p className="text-xs text-muted" dir="ltr">
          {CONTENT_HELP_PAGE_HREF}
        </p>
      </header>

      <nav className="rounded-2xl border bg-surface-elevated p-4" aria-label={isEn ? "Topics" : "موضوعات"}>
        <ul className="flex flex-wrap gap-2">
          {topics.map((topic) => (
            <li key={topic.key}>
              <a
                href={`#${topic.key}`}
                className="inline-block rounded-lg border px-2.5 py-1 text-xs hover:bg-slate-50"
              >
                {contentHelpTitle(topic, locale)}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      <div className="space-y-4">
        {topics.map((topic) => (
          <section
            key={topic.key}
            id={topic.key}
            className="scroll-mt-4 rounded-2xl border bg-surface-elevated p-4"
            data-testid={`content-help-topic-${topic.key}`}
          >
            <h2 className="text-base font-semibold">{contentHelpTitle(topic, locale)}</h2>
            <dl className="mt-3 space-y-2 text-sm">
              <div>
                <dt className="font-medium text-muted">{isEn ? "What it is" : "چیست"}</dt>
                <dd>{isEn ? topic.whatEn : topic.whatFa}</dd>
              </div>
              <div>
                <dt className="font-medium text-muted">{isEn ? "Why it matters" : "چرا مهم است"}</dt>
                <dd>{isEn ? topic.whyEn : topic.whyFa}</dd>
              </div>
              <div>
                <dt className="font-medium text-muted">{isEn ? "What to do" : "چه کار کنید"}</dt>
                <dd>{isEn ? topic.doEn : topic.doFa}</dd>
              </div>
            </dl>
          </section>
        ))}
      </div>
    </main>
  );
}
