"use client";

import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { toast } from "react-toastify";
import { formatJalaliDate, useAdminFormMode } from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import {
  deactivateContentAuthor,
  fetchContentAuthorWorkspace,
  mapContentAuthorMutationError,
  updateContentAuthorAbout,
  updateContentAuthorCore,
  updateContentAuthorMedia,
  updateContentAuthorSocial,
  type ContentAuthorWorkspaceDto,
} from "./content-author-api.ts";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "./media-api.ts";

const TABS = [
  { id: "general", label: "عمومی" },
  { id: "about", label: "درباره نویسنده" },
  { id: "media", label: "رسانه" },
  { id: "social", label: "شبکه‌های اجتماعی" },
  { id: "articles", label: "مقالات" },
  { id: "history", label: "تاریخچه" },
] as const;

type TabId = (typeof TABS)[number]["id"];
type MediaTarget = "profile" | "cover" | null;

export function ContentAuthorAdminScreen() {
  const params = useParams<{ authorId?: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const authorId = typeof params.authorId === "string" ? params.authorId : null;
  const [workspace, setWorkspace] = useState<ContentAuthorWorkspaceDto | null>(null);
  const [tab, setTab] = useState<TabId>("general");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [draftDisplayName, setDraftDisplayName] = useState("");
  const [draftSlug, setDraftSlug] = useState("");
  const [draftIsActive, setDraftIsActive] = useState(true);
  const [draftShortBio, setDraftShortBio] = useState("");
  const [draftFullBio, setDraftFullBio] = useState("");
  const [draftWebsiteUrl, setDraftWebsiteUrl] = useState("");
  const [draftInstagramUrl, setDraftInstagramUrl] = useState("");
  const [draftTwitterUrl, setDraftTwitterUrl] = useState("");
  const [draftLinkedInUrl, setDraftLinkedInUrl] = useState("");
  const [profileAsset, setProfileAsset] = useState<MediaAssetDto | null>(null);
  const [coverAsset, setCoverAsset] = useState<MediaAssetDto | null>(null);
  const [mediaOpen, setMediaOpen] = useState(false);
  const [mediaTarget, setMediaTarget] = useState<MediaTarget>(null);

  const form = useAdminFormMode({ canView: true, canEdit: true });

  useEffect(() => {
    if (searchParams.get("mode") === "edit") {
      form.onEdit();
    }
  }, [searchParams, form]);

  const applyWorkspace = useCallback((data: ContentAuthorWorkspaceDto) => {
    setWorkspace(data);
    setDraftDisplayName(data.displayName);
    setDraftSlug(data.slug);
    setDraftIsActive(data.isActive);
    setDraftShortBio(data.shortBio ?? "");
    setDraftFullBio(data.fullBio ?? "");
    setDraftWebsiteUrl(data.websiteUrl ?? "");
    setDraftInstagramUrl(data.instagramUrl ?? "");
    setDraftTwitterUrl(data.twitterUrl ?? "");
    setDraftLinkedInUrl(data.linkedInUrl ?? "");
    setProfileAsset(
      data.profileImageMediaAssetId
        ? ({ mediaAssetId: data.profileImageMediaAssetId } as MediaAssetDto)
        : null,
    );
    setCoverAsset(
      data.coverImageMediaAssetId
        ? ({ mediaAssetId: data.coverImageMediaAssetId } as MediaAssetDto)
        : null,
    );
  }, []);

  const refreshWorkspace = useCallback(async (id: string) => {
    const result = await fetchContentAuthorWorkspace(id);
    if (result.state !== "ok" || !result.data) {
      setWorkspace(null);
      return;
    }
    applyWorkspace(result.data);
  }, [applyWorkspace]);

  useEffect(() => {
    if (!authorId) {
      setWorkspace(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    void prepareAdminDevActor()
      .then(() => refreshWorkspace(authorId))
      .finally(() => setLoading(false));
  }, [authorId, refreshWorkspace]);

  const saveGeneral = useCallback(async () => {
    if (!workspace) return;
    setSaving(true);
    const result = await updateContentAuthorCore(workspace.authorId, {
      displayName: draftDisplayName,
      slug: draftSlug,
      isActive: draftIsActive,
    });
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    toast.success("ذخیره شد");
    form.onSaved();
    applyWorkspace(result.data);
  }, [applyWorkspace, draftDisplayName, draftIsActive, draftSlug, form, workspace]);

  const saveAbout = useCallback(async () => {
    if (!workspace) return;
    setSaving(true);
    const result = await updateContentAuthorAbout(workspace.authorId, {
      shortBio: draftShortBio || null,
      fullBio: draftFullBio || null,
    });
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    toast.success("بیوگرافی ذخیره شد");
    form.onSaved();
    applyWorkspace(result.data);
  }, [applyWorkspace, draftFullBio, draftShortBio, form, workspace]);

  const saveSocial = useCallback(async () => {
    if (!workspace) return;
    setSaving(true);
    const result = await updateContentAuthorSocial(workspace.authorId, {
      websiteUrl: draftWebsiteUrl || null,
      instagramUrl: draftInstagramUrl || null,
      twitterUrl: draftTwitterUrl || null,
      linkedInUrl: draftLinkedInUrl || null,
    });
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    toast.success("شبکه‌های اجتماعی ذخیره شد");
    form.onSaved();
    applyWorkspace(result.data);
  }, [applyWorkspace, draftInstagramUrl, draftLinkedInUrl, draftTwitterUrl, draftWebsiteUrl, form, workspace]);

  const assignMedia = useCallback(async (target: Exclude<MediaTarget, null>, asset: MediaAssetDto | null) => {
    if (!workspace) return;
    setSaving(true);
    const input =
      target === "profile"
        ? { profileImageMediaAssetId: asset?.mediaAssetId ?? null }
        : { coverImageMediaAssetId: asset?.mediaAssetId ?? null };
    const result = await updateContentAuthorMedia(workspace.authorId, input);
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    if (target === "profile") setProfileAsset(asset);
    else setCoverAsset(asset);
    setMediaOpen(false);
    setMediaTarget(null);
    toast.success(asset ? "تصویر اختصاص یافت" : "تصویر حذف شد");
    applyWorkspace(result.data);
  }, [applyWorkspace, workspace]);

  const deactivateSelected = useCallback(async () => {
    if (!workspace) return;
    if (!window.confirm(`غیرفعال‌سازی «${workspace.displayName}»؟`)) return;
    setSaving(true);
    const result = await deactivateContentAuthor(workspace.authorId);
    setSaving(false);
    if (result.state !== "ok") {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    toast.success("غیرفعال شد");
    router.push("/admin/content/authors");
  }, [router, workspace]);

  const openMediaPicker = useCallback((target: Exclude<MediaTarget, null>) => {
    setMediaTarget(target);
    setMediaOpen(true);
  }, []);

  if (!authorId) {
    return <p className="text-sm text-muted">شناسهٔ نویسنده نامعتبر است.</p>;
  }

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری…</p>;
  }

  if (!workspace) {
    return (
      <div className="space-y-3 text-sm">
        <p className="text-muted">نویسنده یافت نشد.</p>
        <Link href="/admin/content/authors" className="text-[#2563EB] underline">بازگشت به فهرست</Link>
      </div>
    );
  }

  return (
    <main className="w-full" data-testid="admin-content-author-workspace">
      <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
        <div>
          <Link href="/admin/content/authors" className="text-sm text-[#2563EB] underline">← فهرست نویسندگان</Link>
          <h1 className="mt-2 text-[length:var(--type-title)] font-semibold tracking-tight">{workspace.displayName}</h1>
          <p className="mt-1 text-sm text-muted" dir="ltr">{workspace.slug}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {form.mode === "view" ? (
            <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={form.onEdit}>ویرایش</button>
          ) : (
            <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={form.onCancel}>انصراف</button>
          )}
          {workspace.isActive ? (
            <button
              type="button"
              className="rounded-xl border border-red-200 px-3 py-2 text-sm text-red-700"
              onClick={() => void deactivateSelected()}
              disabled={saving}
            >
              غیرفعال‌سازی
            </button>
          ) : null}
        </div>
      </div>

      <section className="rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm">
        <div className="mb-4 flex flex-wrap gap-2 border-b border-border pb-2">
          {TABS.map((item) => (
            <button
              key={item.id}
              type="button"
              className={`rounded-lg px-3 py-1.5 text-sm ${tab === item.id ? "bg-slate-900 text-white" : "text-muted"}`}
              onClick={() => setTab(item.id)}
              data-testid={`content-author-tab-${item.id}`}
            >
              {item.label}
            </button>
          ))}
        </div>

        {tab === "general" ? (
          <div className="space-y-3">
            <label className="block text-sm">
              <span className="mb-1 block text-muted">نام نمایشی</span>
              <input className="w-full rounded-xl border px-3 py-2" value={draftDisplayName} disabled={form.mode === "view"} onChange={(e) => setDraftDisplayName(e.target.value)} />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">نامک</span>
              <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={draftSlug} disabled={form.mode === "view"} onChange={(e) => setDraftSlug(e.target.value)} />
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={draftIsActive} disabled={form.mode === "view"} onChange={(e) => setDraftIsActive(e.target.checked)} />
              <span>فعال</span>
            </label>
            {form.mode !== "view" ? (
              <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white" disabled={saving} onClick={() => void saveGeneral()}>
                ذخیره عمومی
              </button>
            ) : null}
          </div>
        ) : null}

        {tab === "about" ? (
          <div className="space-y-3">
            <label className="block text-sm">
              <span className="mb-1 block text-muted">بیوگرافی کوتاه</span>
              <textarea className="w-full rounded-xl border px-3 py-2" rows={3} value={draftShortBio} disabled={form.mode === "view"} onChange={(e) => setDraftShortBio(e.target.value)} />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">بیوگرافی کامل</span>
              <textarea className="w-full rounded-xl border px-3 py-2" rows={6} value={draftFullBio} disabled={form.mode === "view"} onChange={(e) => setDraftFullBio(e.target.value)} />
            </label>
            {form.mode !== "view" ? (
              <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white" disabled={saving} onClick={() => void saveAbout()}>
                ذخیره بیوگرافی
              </button>
            ) : null}
          </div>
        ) : null}

        {tab === "media" ? (
          <div className="grid gap-6 md:grid-cols-2">
            <div className="space-y-3">
              <h3 className="text-sm font-semibold">تصویر پروفایل</h3>
              {profileAsset?.mediaAssetId ? (
                <img src={mediaPreviewUrl(profileAsset.mediaAssetId) ?? ""} alt="" className="max-h-40 rounded-xl border object-cover" />
              ) : (
                <p className="text-sm text-muted">تصویری اختصاص داده نشده است.</p>
              )}
              <div className="flex gap-2">
                <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={() => openMediaPicker("profile")} disabled={form.mode === "view"}>
                  انتخاب از کتابخانه
                </button>
                {profileAsset ? (
                  <button type="button" className="rounded-xl border px-3 py-2 text-sm" disabled={form.mode === "view"} onClick={() => void assignMedia("profile", null)}>
                    حذف اختصاص
                  </button>
                ) : null}
              </div>
            </div>
            <div className="space-y-3">
              <h3 className="text-sm font-semibold">تصویر کاور</h3>
              {coverAsset?.mediaAssetId ? (
                <img src={mediaPreviewUrl(coverAsset.mediaAssetId) ?? ""} alt="" className="max-h-40 w-full rounded-xl border object-cover" />
              ) : (
                <p className="text-sm text-muted">کاوری اختصاص داده نشده است.</p>
              )}
              <div className="flex gap-2">
                <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={() => openMediaPicker("cover")} disabled={form.mode === "view"}>
                  انتخاب از کتابخانه
                </button>
                {coverAsset ? (
                  <button type="button" className="rounded-xl border px-3 py-2 text-sm" disabled={form.mode === "view"} onClick={() => void assignMedia("cover", null)}>
                    حذف اختصاص
                  </button>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}

        {tab === "social" ? (
          <div className="space-y-3">
            {([
              ["draftWebsiteUrl", "وب‌سایت"],
              ["draftInstagramUrl", "اینستاگرام"],
              ["draftTwitterUrl", "توییتر / X"],
              ["draftLinkedInUrl", "لینکدین"],
            ] as const).map(([key, label]) => (
              <label key={key} className="block text-sm">
                <span className="mb-1 block text-muted">{label}</span>
                <input
                  className="w-full rounded-xl border px-3 py-2"
                  dir="ltr"
                  value={key === "draftWebsiteUrl" ? draftWebsiteUrl : key === "draftInstagramUrl" ? draftInstagramUrl : key === "draftTwitterUrl" ? draftTwitterUrl : draftLinkedInUrl}
                  disabled={form.mode === "view"}
                  onChange={(e) => {
                    if (key === "draftWebsiteUrl") setDraftWebsiteUrl(e.target.value);
                    else if (key === "draftInstagramUrl") setDraftInstagramUrl(e.target.value);
                    else if (key === "draftTwitterUrl") setDraftTwitterUrl(e.target.value);
                    else setDraftLinkedInUrl(e.target.value);
                  }}
                />
              </label>
            ))}
            {form.mode !== "view" ? (
              <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white" disabled={saving} onClick={() => void saveSocial()}>
                ذخیره شبکه‌های اجتماعی
              </button>
            ) : null}
          </div>
        ) : null}

        {tab === "articles" ? (
          <div className="space-y-3 text-sm">
            <p>تعداد مقالات متصل: <strong>{workspace.articleCount}</strong></p>
            <Link href="/admin/content" className="text-[#2563EB] underline">رفتن به فهرست مقالات</Link>
          </div>
        ) : null}

        {tab === "history" ? (
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl border p-3 text-sm">
              <div className="text-muted">ایجاد</div>
              <div>{formatJalaliDate(workspace.createdAt, "fa")}</div>
            </div>
            <div className="rounded-xl border p-3 text-sm">
              <div className="text-muted">آخرین به‌روزرسانی</div>
              <div>{formatJalaliDate(workspace.updatedAt, "fa")}</div>
            </div>
          </div>
        ) : null}
      </section>

      <MediaLibraryDialog
        open={mediaOpen}
        selectionMode="single"
        onClose={() => {
          setMediaOpen(false);
          setMediaTarget(null);
        }}
        onConfirm={(assets) => {
          if (mediaTarget) void assignMedia(mediaTarget, assets[0] ?? null);
        }}
      />
    </main>
  );
}
