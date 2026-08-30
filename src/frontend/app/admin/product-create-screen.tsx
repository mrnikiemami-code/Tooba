"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Button, buildCategoryPath, type AppCategoryTreeNode } from "../../design-system";
import { fetchCategoryTree, slugifyCategoryName, type CategoryTreeNodeDto } from "./catalog-category-api";
import {
  assignAdminProductBrand,
  createAdminProduct,
  listAdminBrandOptions,
  loadProductWorkspace,
  updateAdminProductCore,
  type AdminBrandOption,
} from "./host-client";
import { ProductCategoryPicker } from "./product-category-picker";
import { PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA } from "./product-category-level";
import { ProductRichTextEditor } from "./product-rich-text-editor";
import { mapAdminErrorMessage } from "./admin-error-map";
import { sanitizeProductRichHtml } from "./product-rich-html";
import { ProductAttributesPanel } from "./product-attributes-panel";
import { ProductVariantsPanel } from "./product-variants-panel";
import { ProductMediaPanel } from "./product-media-panel";
import { ProductSeoPanel } from "./product-seo-panel";
import { translationReadiness } from "./product-translations-readiness";
import type { ProductWorkspaceView } from "./workspace-model";
import { AdminSearchableCombobox } from "./admin-searchable-combobox";

const STEPS = [
  { id: "category", labelFa: "دسته اصلی", labelEn: "Primary Category" },
  { id: "structure", labelFa: "اطلاعات پایه", labelEn: "Base structure" },
  { id: "translations", labelFa: "ترجمه‌ها", labelEn: "Translations" },
  { id: "attributes", labelFa: "ویژگی‌ها", labelEn: "Attributes" },
  { id: "variants", labelFa: "تنوع‌ها", labelEn: "Variants" },
  { id: "media", labelFa: "رسانه", labelEn: "Media" },
  { id: "seo", labelFa: "SEO", labelEn: "SEO" },
  { id: "review", labelFa: "بررسی و ایجاد", labelEn: "Review & create" },
] as const;

type StepId = (typeof STEPS)[number]["id"];

type LocaleDraft = {
  name: string;
  shortDescription: string;
  descriptionHtml: string;
};

/**
 * ایجاد محصول ۸مرحله‌ای — Draft-first پس از ترجمه‌ها؛ پنل‌های واقعی Attributes/Variants/Media/SEO.
 */
export function ProductCreateScreen() {
  const router = useRouter();
  const [step, setStep] = useState<StepId>("category");
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [treeNodes, setTreeNodes] = useState<AppCategoryTreeNode[]>([]);
  const [brandId, setBrandId] = useState<string | null>(null);
  const [brands, setBrands] = useState<AdminBrandOption[]>([]);
  const [slug, setSlug] = useState("");
  const [slugTouched, setSlugTouched] = useState(false);
  const [fa, setFa] = useState<LocaleDraft>({ name: "", shortDescription: "", descriptionHtml: "" });
  const [en, setEn] = useState<LocaleDraft>({ name: "", shortDescription: "", descriptionHtml: "" });
  const [productId, setProductId] = useState<string | null>(null);
  const [workspace, setWorkspace] = useState<ProductWorkspaceView | null>(null);
  const [draftBanner, setDraftBanner] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void fetchCategoryTree("fa-IR").then((result) => {
      if (result.state !== "ok" || !result.data) return;
      setTreeNodes(
        result.data.map((r: CategoryTreeNodeDto) => ({
          id: r.id,
          parentId: r.parentId,
          name: r.name,
          slug: r.slug,
          status: r.status,
          sortOrder: r.sortOrder,
          isVisible: r.isVisible,
          hasChildren: r.hasChildren,
          productCount: r.productCount,
        })),
      );
    });
    void listAdminBrandOptions().then((result) => {
      if (result.ok) setBrands(result.items);
    });
  }, []);

  const categoryPath = categoryId ? buildCategoryPath(treeNodes, categoryId).join(" > ") : "";
  const stepIndex = STEPS.findIndex((s) => s.id === step);
  const brandName = brands.find((b) => b.brandId === brandId)?.name ?? null;

  const faReady = translationReadiness({
    name: fa.name,
    shortDescription: fa.shortDescription,
    description: fa.descriptionHtml,
  });
  const enReady = translationReadiness({
    name: en.name,
    shortDescription: en.shortDescription,
    description: en.descriptionHtml,
  });

  const canNext = useMemo(() => {
    if (step === "category") return Boolean(categoryId);
    if (step === "structure") return true;
    if (step === "translations") return fa.name.trim().length > 0;
    return true;
  }, [step, categoryId, fa.name]);

  const refreshWorkspace = useCallback(async (id: string) => {
    const loaded = await loadProductWorkspace(id, false);
    if (loaded.view) setWorkspace(loaded.view);
    return loaded.view;
  }, []);

  async function ensureDraft(): Promise<string | null> {
    if (productId) return productId;
    if (!categoryId || !fa.name.trim()) {
      setError("دسته اصلی و نام فارسی لازم است.");
      return null;
    }
    setBusy(true);
    setError(null);
    const created = await createAdminProduct({
      title: fa.name.trim(),
      slug: slug.trim() || null,
      categoryId,
      locale: "fa-IR",
    });
    if (!created.ok) {
      setBusy(false);
      setError(
        created.errorCode === "workspace.product.category.level.invalid"
          ? PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA
          : mapAdminErrorMessage(created.errorCode, "fa"),
      );
      return null;
    }

    let view = await refreshWorkspace(created.productId);
    if (view) {
      const faUpdate = await updateAdminProductCore(created.productId, {
        locale: "fa-IR",
        title: fa.name.trim(),
        slug: slug.trim() || null,
        shortDescription: fa.shortDescription.trim() || null,
        description: sanitizeProductRichHtml(fa.descriptionHtml) || null,
        expectedUpdatedAt: view.catalogUpdatedAt,
      });
      if (faUpdate.ok) {
        view = faUpdate.view;
        setWorkspace(faUpdate.view);
      }
    }

    if (view && en.name.trim()) {
      const enUpdate = await updateAdminProductCore(created.productId, {
        locale: "en",
        title: en.name.trim(),
        slug: view.slug ?? (slug.trim() || null),
        shortDescription: en.shortDescription.trim() || null,
        description: sanitizeProductRichHtml(en.descriptionHtml) || null,
        expectedUpdatedAt: view.catalogUpdatedAt,
      });
      if (enUpdate.ok) {
        view = enUpdate.view;
        setWorkspace(enUpdate.view);
      }
    }

    if (brandId && view) {
      const brandResult = await assignAdminProductBrand(
        created.productId,
        { brandId, expectedUpdatedAt: view.catalogUpdatedAt },
        false,
      );
      if (brandResult.ok) {
        setWorkspace(brandResult.view);
      }
    }

    setProductId(created.productId);
    setDraftBanner(true);
    setBusy(false);
    return created.productId;
  }

  async function goNext() {
    setError(null);
    if (step === "category") {
      setStep("structure");
      return;
    }
    if (step === "structure") {
      setStep("translations");
      return;
    }
    if (step === "translations") {
      const id = await ensureDraft();
      if (!id) return;
      setStep("attributes");
      return;
    }
    if (step === "attributes") {
      setStep("variants");
      return;
    }
    if (step === "variants") {
      setStep("media");
      return;
    }
    if (step === "media") {
      setStep("seo");
      return;
    }
    if (step === "seo") {
      if (productId) await refreshWorkspace(productId);
      setStep("review");
    }
  }

  function goBack() {
    setError(null);
    const order = STEPS.map((s) => s.id);
    const idx = order.indexOf(step);
    if (idx > 0) setStep(order[idx - 1]!);
  }

  function goToStep(target: StepId) {
    const order = STEPS.map((s) => s.id);
    const targetIdx = order.indexOf(target);
    const attrsIdx = order.indexOf("attributes");
    if (targetIdx >= attrsIdx && !productId) {
      setError("ابتدا ترجمه‌ها را تکمیل کنید تا پیش‌نویس ساخته شود.");
      return;
    }
    setStep(target);
  }

  async function onComplete() {
    if (!productId) {
      const id = await ensureDraft();
      if (!id) return;
      router.push(`/admin/products/${id}?scope=edit`);
      return;
    }
    router.push(`/admin/products/${productId}?scope=edit`);
  }

  const readiness = workspace?.publication.aggregateReadiness ?? null;

  return (
    <main className="mx-auto w-full max-w-5xl" data-testid="admin-product-create-screen">
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-medium text-slate-500">
            <Link href="/admin/products" className="hover:text-blue-600">
              فهرست محصولات
            </Link>
            <span className="mx-1">/</span>
            افزودن محصول
          </p>
          <h1 className="mt-1 text-2xl font-bold tracking-tight text-slate-900">ایجاد محصول جدید</h1>
          <p className="mt-1 text-sm text-slate-600">
            جریان هدایت‌شده ۸مرحله‌ای · محصول به‌صورت پیش‌نویس ساخته می‌شود · قیمت و موجودی روی پیشنهاد فروشنده است.
          </p>
        </div>
        <Link
          href="/admin/products"
          className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
          data-testid="admin-product-create-back"
        >
          بازگشت به فهرست
        </Link>
      </div>

      {draftBanner && productId ? (
        <div
          className="mb-4 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-950"
          data-testid="admin-product-create-draft-banner"
          role="status"
        >
          <p className="font-semibold">پیش‌نویس محصول ایجاد شد؛ می‌توانید اطلاعات را تکمیل کنید.</p>
          <p className="mt-1 text-emerald-900/80" dir="ltr">
            Draft id: {productId}
          </p>
        </div>
      ) : null}

      <ol
        className="mb-6 grid gap-2 sm:grid-cols-2 lg:grid-cols-4"
        data-testid="admin-product-create-steps"
        aria-label="مراحل ایجاد محصول"
      >
        {STEPS.map((s, index) => {
          const active = s.id === step;
          const done = index < stepIndex;
          return (
            <li key={s.id}>
              <button
                type="button"
                className={
                  active
                    ? "w-full rounded-2xl border border-blue-200 bg-blue-50 px-3 py-3 text-start text-sm font-semibold text-blue-900"
                    : done
                      ? "w-full rounded-2xl border border-emerald-200 bg-emerald-50/70 px-3 py-3 text-start text-sm font-medium text-emerald-900 hover:brightness-95"
                      : "w-full rounded-2xl border border-gray-200 bg-white px-3 py-3 text-start text-sm text-slate-500 hover:bg-slate-50"
                }
                data-testid={`admin-product-create-step-${s.id}`}
                aria-current={active ? "step" : undefined}
                onClick={() => goToStep(s.id)}
              >
                <span className="block text-[11px] opacity-70">
                  مرحله {index + 1} · Step {index + 1}
                </span>
                <span className="block">{s.labelFa}</span>
                <span className="mt-0.5 block text-[11px] font-normal opacity-70" dir="ltr">
                  {s.labelEn}
                </span>
              </button>
            </li>
          );
        })}
      </ol>

      <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm md:p-6">
        {step === "category" ? (
          <div className="space-y-4" data-testid="admin-product-create-panel-category">
            <h2 className="text-lg font-semibold text-slate-900">دسته اصلی محصول را انتخاب کنید</h2>
            <p className="text-sm text-slate-600">
              دسته اصلی مشخص می‌کند چه ویژگی‌ها و تنوع‌هایی برای محصول قابل استفاده باشند.
            </p>
            <ProductCategoryPicker
              value={categoryId}
              onChange={setCategoryId}
              required
              label="دسته اصلی (سطح سوم)"
            />
            {categoryPath ? (
              <p
                className="rounded-xl bg-slate-50 px-3 py-2 text-sm text-slate-700"
                data-testid="admin-product-create-category-path"
              >
                مسیر: {categoryPath}
              </p>
            ) : null}
          </div>
        ) : null}

        {step === "structure" ? (
          <div className="space-y-4" data-testid="admin-product-create-panel-structure">
            <h2 className="text-lg font-semibold text-slate-900">ساختار زبان‌خنثی محصول</h2>
            <p className="text-sm text-slate-600">
              نامک سراسری است. برند اختیاری است و بعداً هم قابل تغییر است.
            </p>
            <label className="block text-sm font-medium text-slate-700">
              نامک (Slug)
              <input
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 px-3 text-sm"
                dir="ltr"
                value={slug}
                onChange={(e) => {
                  setSlugTouched(true);
                  setSlug(e.target.value);
                }}
                data-testid="admin-product-create-slug"
              />
            </label>
            <div className="block text-sm font-medium text-slate-700">
              برند (اختیاری)
              <div className="mt-1">
                <AdminSearchableCombobox
                  value={brandId}
                  options={brands.map((b) => ({
                    value: b.brandId,
                    label: b.name,
                  }))}
                  noneOption={{ value: "", label: "بدون برند" }}
                  placeholder="جستجو و انتخاب برند…"
                  testId="admin-product-create-brand"
                  onChange={(next) => setBrandId(next)}
                />
              </div>
            </div>
            <div className="rounded-xl border border-dashed border-gray-200 bg-slate-50 p-4 text-sm text-slate-600">
              <p>Product ≠ Offer · بدون قیمت و موجودی روی هویت محصول</p>
              <p className="mt-1">
                وضعیت پس از ایجاد: <strong>پیش‌نویس</strong>
              </p>
            </div>
          </div>
        ) : null}

        {step === "translations" ? (
          <div className="space-y-6" data-testid="admin-product-create-panel-translations">
            <div>
              <h2 className="text-lg font-semibold text-slate-900">ترجمه‌ها</h2>
              <p className="mt-1 text-sm text-slate-600">
                فارسی لازم است. انگلیسی را همین‌جا یا بعداً تکمیل کنید. با ادامه، پیش‌نویس ساخته می‌شود.
              </p>
            </div>
            <div className="rounded-2xl border border-gray-100 bg-slate-50/60 p-4" dir="rtl">
              <div className="mb-3 flex items-center justify-between gap-2">
                <p className="text-sm font-semibold text-slate-800">فارسی (fa-IR)</p>
                <span className="rounded-full bg-white px-2 py-0.5 text-[11px] text-slate-600">{faReady}</span>
              </div>
              <label className="block text-sm font-medium text-slate-700">
                نام محصول
                <input
                  className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm"
                  value={fa.name}
                  onChange={(e) => {
                    const next = e.target.value;
                    setFa((prev) => ({ ...prev, name: next }));
                    if (!slugTouched) setSlug(slugifyCategoryName(next));
                  }}
                  data-testid="admin-product-create-title"
                />
              </label>
              <label className="mt-3 block text-sm font-medium text-slate-700">
                خلاصه کوتاه
                <textarea
                  className="mt-1 min-h-20 w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm"
                  value={fa.shortDescription}
                  onChange={(e) => setFa((prev) => ({ ...prev, shortDescription: e.target.value }))}
                  data-testid="admin-product-create-short"
                />
              </label>
              <div className="mt-3">
                <p className="mb-1 text-sm font-medium text-slate-700">توضیح کامل</p>
                <ProductRichTextEditor
                  value={fa.descriptionHtml}
                  onChange={(html) => setFa((prev) => ({ ...prev, descriptionHtml: html }))}
                  dir="rtl"
                  testId="admin-product-create-description"
                />
              </div>
            </div>
            <div className="rounded-2xl border border-gray-100 bg-slate-50/60 p-4" dir="ltr">
              <div className="mb-3 flex items-center justify-between gap-2">
                <p className="text-sm font-semibold text-slate-800">English (en)</p>
                <span className="rounded-full bg-white px-2 py-0.5 text-[11px] text-slate-600">{enReady}</span>
              </div>
              <label className="block text-sm font-medium text-slate-700">
                Product name
                <input
                  className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm"
                  value={en.name}
                  onChange={(e) => setEn((prev) => ({ ...prev, name: e.target.value }))}
                  data-testid="admin-product-create-title-en"
                />
              </label>
              <label className="mt-3 block text-sm font-medium text-slate-700">
                Short summary
                <textarea
                  className="mt-1 min-h-20 w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm"
                  value={en.shortDescription}
                  onChange={(e) => setEn((prev) => ({ ...prev, shortDescription: e.target.value }))}
                  data-testid="admin-product-create-short-en"
                />
              </label>
              <div className="mt-3">
                <p className="mb-1 text-sm font-medium text-slate-700">Full description</p>
                <ProductRichTextEditor
                  value={en.descriptionHtml}
                  onChange={(html) => setEn((prev) => ({ ...prev, descriptionHtml: html }))}
                  dir="ltr"
                  testId="admin-product-create-description-en"
                />
              </div>
            </div>
          </div>
        ) : null}

        {step === "attributes" && productId ? (
          <div className="space-y-3" data-testid="admin-product-create-panel-attributes">
            <h2 className="text-lg font-semibold text-slate-900">ویژگی‌ها</h2>
            <p className="text-sm text-slate-600">بر اساس شِمای دسته اصلی. اگر ویژگی‌ای نباشد می‌توانید ادامه دهید.</p>
            <ProductAttributesPanel
              productId={productId}
              categoryId={categoryId}
              categoryPath={categoryPath || null}
              canEdit
              mode="edit"
            />
          </div>
        ) : null}

        {step === "variants" && productId ? (
          <div className="space-y-3" data-testid="admin-product-create-panel-variants">
            <h2 className="text-lg font-semibold text-slate-900">تنوع‌ها</h2>
            <p className="text-sm text-slate-600">بدون قیمت و موجودی. اگر ویژگی تنوع نباشد، حالت خالی قابل ادامه است.</p>
            <ProductVariantsPanel productId={productId} categoryId={categoryId} canEdit mode="edit" />
          </div>
        ) : null}

        {step === "media" && productId ? (
          <div className="space-y-3" data-testid="admin-product-create-panel-media">
            <h2 className="text-lg font-semibold text-slate-900">رسانه</h2>
            <p className="text-sm text-slate-600">کتابخانه و آپلود واقعی Media DAM · تصویر اصلی، ترتیب و AltText.</p>
            <ProductMediaPanel productId={productId} canEdit mode="edit" />
          </div>
        ) : null}

        {step === "seo" && productId ? (
          <div className="space-y-3" data-testid="admin-product-create-panel-seo">
            <h2 className="text-lg font-semibold text-slate-900">SEO</h2>
            <p className="text-sm text-slate-600">عنوان جستجو، توضیح متا و پیش‌نمایش. نامک سراسری است.</p>
            <ProductSeoPanel productId={productId} canEdit mode="edit" />
          </div>
        ) : null}

        {step === "review" ? (
          <div className="space-y-4" data-testid="admin-product-create-panel-review">
            <h2 className="text-lg font-semibold text-slate-900">بررسی و تکمیل پیش‌نویس</h2>
            <dl className="grid gap-3 sm:grid-cols-2">
              <ReviewCard label="دسته اصلی" value={categoryPath || categoryId || "—"} />
              <ReviewCard label="برند" value={brandName || workspace?.brandName || "بدون برند"} />
              <ReviewCard label="ترجمه فارسی" value={faReady} />
              <ReviewCard label="ترجمه English" value={enReady} />
              <ReviewCard
                label="ویژگی‌ها"
                value={
                  readiness
                    ? readiness.attributeReady
                      ? "آماده"
                      : "ناقص / قابل بهبود"
                    : productId
                      ? "در Workspace تکمیل کنید"
                      : "—"
                }
              />
              <ReviewCard
                label="تنوع‌ها"
                value={
                  readiness
                    ? readiness.variantReady
                      ? "آماده"
                      : "ناقص / قابل بهبود"
                    : workspace
                      ? `${workspace.variants.length} تنوع`
                      : "—"
                }
              />
              <ReviewCard
                label="رسانه"
                value={
                  readiness
                    ? readiness.mediaReady
                      ? "آماده"
                      : "ناقص / قابل بهبود"
                    : workspace
                      ? `${workspace.media.length} مورد`
                      : "—"
                }
              />
              <ReviewCard
                label="SEO"
                value={
                  readiness
                    ? readiness.seoReady
                      ? "آماده"
                      : "قابل بهبود"
                    : "—"
                }
              />
              <ReviewCard label="وضعیت انتشار" value="پیش‌نویس (منتشر نمی‌شود)" />
              <ReviewCard label="نامک" value={slug || workspace?.slug || "—"} ltr />
            </dl>
            <div className="flex flex-wrap gap-2">
              {(
                [
                  ["category", "دسته"],
                  ["translations", "ترجمه‌ها"],
                  ["attributes", "ویژگی‌ها"],
                  ["variants", "تنوع‌ها"],
                  ["media", "رسانه"],
                  ["seo", "SEO"],
                ] as const
              ).map(([id, label]) => (
                <button
                  key={id}
                  type="button"
                  className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
                  onClick={() => goToStep(id)}
                >
                  بازگشت به {label}
                </button>
              ))}
            </div>
            <p className="text-sm text-slate-600">
              با تکمیل، فضای کاری محصول در حالت ویرایش باز می‌شود. انتشار جداگانه از تب انتشار انجام می‌شود.
            </p>
          </div>
        ) : null}

        {error ? (
          <p className="mt-4 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">
            {error}
          </p>
        ) : null}

        <div className="mt-6 flex flex-wrap items-center justify-between gap-2 border-t border-gray-100 pt-4">
          <div>
            {step !== "category" ? (
              <Button type="button" tone="secondary" disabled={busy} onClick={goBack} data-testid="admin-product-create-prev">
                مرحله قبل
              </Button>
            ) : null}
          </div>
          <div className="flex flex-wrap gap-2">
            {step !== "review" ? (
              <Button
                type="button"
                disabled={!canNext || busy}
                onClick={() => void goNext()}
                data-testid="admin-product-create-next"
              >
                {busy && step === "translations" ? "در حال ایجاد پیش‌نویس…" : "ادامه"}
              </Button>
            ) : (
              <Button
                type="button"
                disabled={busy}
                onClick={() => void onComplete()}
                data-testid="admin-product-create-submit"
              >
                {busy ? "در حال انتقال…" : "تکمیل و باز کردن فضای کاری"}
              </Button>
            )}
          </div>
        </div>
      </section>
    </main>
  );
}

function ReviewCard({ label, value, ltr }: { label: string; value: string; ltr?: boolean }) {
  return (
    <div className="rounded-xl border border-gray-100 bg-slate-50 p-3">
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="mt-1 text-sm font-medium text-slate-800" dir={ltr ? "ltr" : undefined}>
        {value}
      </dd>
    </div>
  );
}
