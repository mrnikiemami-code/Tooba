/**
 * کلاینت Admin برای نظرات مقاله و تعدیل.
 */
import { parseAdminProblemErrorCode } from "./admin-error-map.ts";
import { prepareAdminDevActor } from "./admin-api.ts";

export type ArticleCommentStatus = "Pending" | "Approved" | "Rejected" | "Hidden";

export type ArticleCommentRow = {
  id: string;
  commentId: string;
  articleId: string;
  authorPartyId: string | null;
  displayName: string;
  body: string;
  status: ArticleCommentStatus;
  createdAt: string;
  moderatedAt: string | null;
  moderatedByUserId: string | null;
  moderationNote: string | null;
};

export type ArticleCommentPage = {
  items: ArticleCommentRow[];
  totalCount: number;
  skip: number;
  take: number;
  pendingCount: number;
};

type AdminResult<T> =
  | { state: "ok"; data: T; status: number }
  | { state: "denied" | "error"; data: null; status: number; message: string };

function adminHeaders(json = false): HeadersInit {
  prepareAdminDevActor();
  const headers: Record<string, string> = {
    Accept: "application/json",
  };
  if (json) headers["Content-Type"] = "application/json";
  return headers;
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function prop(root: Record<string, unknown>, camel: string, pascal: string): unknown {
  return root[camel] ?? root[pascal];
}

function text(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function asStatus(value: unknown): ArticleCommentStatus {
  const raw = text(value, "Pending");
  if (raw === "Approved" || raw === "1") return "Approved";
  if (raw === "Rejected" || raw === "2") return "Rejected";
  if (raw === "Hidden" || raw === "3") return "Hidden";
  return "Pending";
}

function mapComment(raw: unknown): ArticleCommentRow | null {
  const root = recordOf(raw);
  if (!root) return null;
  const commentId = text(prop(root, "commentId", "CommentId"));
  const articleId = text(prop(root, "articleId", "ArticleId"));
  if (!commentId || !articleId) return null;
  const authorParty = prop(root, "authorPartyId", "AuthorPartyId");
  return {
    id: commentId,
    commentId,
    articleId,
    authorPartyId: typeof authorParty === "string" && authorParty ? authorParty : null,
    displayName: text(prop(root, "displayName", "DisplayName"), "—"),
    body: text(prop(root, "body", "Body")),
    status: asStatus(prop(root, "status", "Status")),
    createdAt: text(prop(root, "createdAt", "CreatedAt")),
    moderatedAt: text(prop(root, "moderatedAt", "ModeratedAt")) || null,
    moderatedByUserId: text(prop(root, "moderatedByUserId", "ModeratedByUserId")) || null,
    moderationNote: text(prop(root, "moderationNote", "ModerationNote")) || null,
  };
}

function mapPage(raw: unknown): ArticleCommentPage | null {
  const root = recordOf(raw);
  if (!root) return null;
  const itemsRaw = prop(root, "items", "Items");
  const items = Array.isArray(itemsRaw)
    ? itemsRaw.map(mapComment).filter((row): row is ArticleCommentRow => row !== null)
    : [];
  return {
    items,
    totalCount: Number(prop(root, "totalCount", "TotalCount") ?? items.length) || 0,
    skip: Number(prop(root, "skip", "Skip") ?? 0) || 0,
    take: Number(prop(root, "take", "Take") ?? 20) || 20,
    pendingCount: Number(prop(root, "pendingCount", "PendingCount") ?? 0) || 0,
  };
}

async function readResult<T>(
  response: Response,
  map: (raw: unknown) => T | null,
): Promise<AdminResult<T>> {
  if (response.status === 401 || response.status === 403) {
    return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
  }
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    return {
      state: "error",
      data: null,
      status: response.status,
      message: parseAdminProblemErrorCode(payload, response.status),
    };
  }
  const data = map(payload);
  return data
    ? { state: "ok", data, status: response.status }
    : { state: "error", data: null, status: response.status, message: "invalid-response" };
}

/** فهرست صفحه‌بندی‌شدهٔ نظرات مقاله. */
export async function loadArticleComments(
  articleId: string,
  opts: { status?: ArticleCommentStatus | ""; search?: string; skip?: number; take?: number } = {},
): Promise<AdminResult<ArticleCommentPage>> {
  try {
    const qs = new URLSearchParams({
      skip: String(opts.skip ?? 0),
      take: String(opts.take ?? 20),
    });
    if (opts.status) qs.set("status", opts.status);
    if (opts.search?.trim()) qs.set("search", opts.search.trim());
    const response = await fetch(
      `/v1/admin/content/articles/${encodeURIComponent(articleId)}/comments?${qs}`,
      { headers: adminHeaders() },
    );
    return await readResult(response, mapPage);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** ایجاد نظر Pending برای smoke/admin. */
export async function createArticleComment(
  articleId: string,
  input: { displayName: string; body: string },
): Promise<AdminResult<ArticleCommentRow>> {
  try {
    const response = await fetch(
      `/v1/admin/content/articles/${encodeURIComponent(articleId)}/comments`,
      {
        method: "POST",
        headers: adminHeaders(true),
        body: JSON.stringify({
          displayName: input.displayName,
          body: input.body,
        }),
      },
    );
    return await readResult(response, mapComment);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

type ModerateAction = "approve" | "reject" | "hide" | "pending";

async function moderate(
  articleId: string,
  commentId: string,
  action: ModerateAction,
  note?: string,
): Promise<AdminResult<ArticleCommentRow>> {
  try {
    const response = await fetch(
      `/v1/admin/content/articles/${encodeURIComponent(articleId)}/comments/${encodeURIComponent(commentId)}/${action}`,
      {
        method: "POST",
        headers: adminHeaders(true),
        body: JSON.stringify({ note: note ?? null }),
      },
    );
    return await readResult(response, mapComment);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export const approveArticleComment = (articleId: string, commentId: string, note?: string) =>
  moderate(articleId, commentId, "approve", note);
export const rejectArticleComment = (articleId: string, commentId: string, note?: string) =>
  moderate(articleId, commentId, "reject", note);
export const hideArticleComment = (articleId: string, commentId: string, note?: string) =>
  moderate(articleId, commentId, "hide", note);
export const markArticleCommentPending = (articleId: string, commentId: string, note?: string) =>
  moderate(articleId, commentId, "pending", note);

export function articleCommentStatusLabel(status: ArticleCommentStatus, locale: "fa" | "en" = "fa"): string {
  if (locale === "en") {
    return (
      {
        Pending: "Pending",
        Approved: "Approved",
        Rejected: "Rejected",
        Hidden: "Hidden",
      } as const
    )[status];
  }
  return (
    {
      Pending: "در انتظار",
      Approved: "تأییدشده",
      Rejected: "ردشده",
      Hidden: "پنهان",
    } as const
  )[status];
}
