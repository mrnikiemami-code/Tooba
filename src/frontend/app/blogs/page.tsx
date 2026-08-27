import type { Metadata } from "next";
import { BlogsListingClient } from "./blogs-ui";

export const metadata: Metadata = {
  title: "مجله توبا | مقالات و راهنماها",
  description: "مقالات منتشرشدهٔ توبا درباره خرید آنلاین، محصول و راهنماها.",
  alternates: { canonical: "/blogs" },
};

/** مسیر فهرست بلاگ عمومی. */
export default function BlogsPage() {
  return <BlogsListingClient />;
}
