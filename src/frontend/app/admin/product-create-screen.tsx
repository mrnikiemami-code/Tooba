"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { Button, buildCategoryPath, type AppCategoryTreeNode } from "../../design-system";
import { fetchCategoryTree, slugifyCategoryName, type CategoryTreeNodeDto } from "./catalog-category-api";
import { createAdminProduct, updateAdminProductCore, loadProductWorkspace } from "./host-client";
import { ProductCategoryPicker } from "./product-category-picker";
import { PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA } from "./product-category-level";
import { ProductRichTextEditor } from "./product-rich-text-editor";
import { mapAdminErrorMessage } from "./admin-error-map";
import { sanitizeProductRichHtml } from "./product-rich-html";

const STEPS = [
  { id: "category", label: "دسته اصلی" },
  { id: "structure", label: "اطلاعات پایه" },
  { id: "translations", label: "ترجمه‌ها" },
  { id: "review", label: "بررسی و ایجاد" },
] as const;

type StepId = (typeof STEPS)[number]["id"];

/**
 * فضای کاری اختصاصی ایجاد محصول — بدون فرم غول‌پیکر؛ Draft-first پس از تأیید.
 */
export function ProductCreateScreen() {
  const router = useRouter();
  const [step, setStep] = useState<StepId>("category");
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [treeNodes, setTreeNodes] = useState<AppCategoryTreeNode[]>([]);
  const [title, setTitle] = useState("");
  const [slug, setSlug] = useState("");
  const [slugTouched, setSlugTouched] = useState(false);
  const [shortDescription, setShortDescription] = useState("");
  const [descriptionHtml, setDescriptionHtml] = useState("");
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
  }, []);

  const categoryPath = categoryId ? buildCategoryPath(treeNodes, categoryId).join(" > ") : "";
  const stepIndex = STEPS.findIndex((s) => s.id === step);

  const canNext = useMemo(() => {
    if (step === "category") return Boolean(categoryId);
    if (step === "structure") return true;
    if (step === "translations") return title.trim().length > 0;
    return true;
  }, [step, categoryId, title]);

  function goNext() {
    setError(null);
    if (step === "category") setStep("structure");
    else if (step === "structure") setStep("translations");
    else if (step === "translations") setStep("review");
  }

  function goBack() {
    setError(null);
    if (step === "structure") setStep("category");
    else if (step === "translations") setStep("structure");
    else if (step === "review") setStep("translations");
  }

  async function onCreate() {
    if (!categoryId || !title.trim()) {
      setError("دسته اصلی و نام فارسی لازم است.");
      return;
    }
    setBusy(true);
    setError(null);
    const created = await createAdminProduct({
      title: title.trim(),
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
      return;
    }

    const workspace = await loadProductWorkspace(created.productId, false);
    if (workspace.view) {
      const desc = sanitizeProductRichHtml(descriptionHtml);
      await updateAdminProductCore(created.productId, {
        locale: "fa-IR",
        title: title.trim(),
        slug: slug.trim() || null,
        shortDescription: shortDescription.trim() || null,
        description: desc || null,
        expectedUpdatedAt: workspace.view.catalogUpdatedAt,
      });
    }

    setBusy(false);
    router.push(`/admin/products/${created.productId}?scope=edit`);
  }

  return (
    <main className="mx-auto w-full max-w-4xl" data-testid="admin-product-create-screen">
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
            محصول به‌صورت پیش‌نویس ساخته می‌شود. قیمت و موجودی متعلق به پیشنهاد فروشنده است، نه هویت محصول.
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

      <ol
        className="mb-6 grid gap-2 sm:grid-cols-4"
        data-testid="admin-product-create-steps"
        aria-label="مراحل ایجاد محصول"
      >
        {STEPS.map((s, index) => {
          const active = s.id === step;
          const done = index < stepIndex;
          return (
            <li
              key={s.id}
              className={
                active
                  ? "rounded-2xl border border-blue-200 bg-blue-50 px-3 py-3 text-sm font-semibold text-blue-900"
                  : done
                    ? "rounded-2xl border border-emerald-200 bg-emerald-50/70 px-3 py-3 text-sm font-medium text-emerald-900"
                    : "rounded-2xl border border-gray-200 bg-white px-3 py-3 text-sm text-slate-500"
              }
              data-testid={`admin-product-create-step-${s.id}`}
              aria-current={active ? "step" : undefined}
            >
              <span className="block text-[11px] opacity-70">مرحله {index + 1}</span>
              {s.label}
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
              <p className="rounded-xl bg-slate-50 px-3 py-2 text-sm text-slate-700" data-testid="admin-product-create-category-path">
                مسیر: {categoryPath}
              </p>
            ) : null}
          </div>
        ) : null}

        {step === "structure" ? (
          <div className="space-y-4" data-testid="admin-product-create-panel-structure">
            <h2 className="text-lg font-semibold text-slate-900">ساختار زبان‌خنثی محصول</h2>
            <p className="text-sm text-slate-600">
              عنوان و توضیح در مرحله ترجمه‌ها وارد می‌شود. برند را می‌توانید بعد از ایجاد در فضای کاری محصول تنظیم کنید.
            </p>
            <div className="rounded-xl border border-dashed border-gray-200 bg-slate-50 p-4 text-sm text-slate-600">
              <p>Product ≠ Offer · بدون قیمت و موجودی روی هویت محصول</p>
              <p className="mt-1">وضعیت پس از ایجاد: <strong>پیش‌نویس</strong></p>
            </div>
          </div>
        ) : null}

        {step === "translations" ? (
          <div className="space-y-4" data-testid="admin-product-create-panel-translations">
            <h2 className="text-lg font-semibold text-slate-900">ترجمه فارسی (اولیه)</h2>
            <p className="text-sm text-slate-600">پس از ایجاد می‌توانید انگلیسی و عربی را در تب ترجمه‌ها تکمیل کنید.</p>
            <label className="block text-sm font-medium text-slate-700">
              نام محصول (فارسی)
              <input
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 px-3 text-sm"
                value={title}
                onChange={(e) => {
                  const next = e.target.value;
                  setTitle(next);
                  if (!slugTouched) setSlug(slugifyCategoryName(next));
                }}
                data-testid="admin-product-create-title"
              />
            </label>
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
            <label className="block text-sm font-medium text-slate-700">
              خلاصه کوتاه
              <textarea
                className="mt-1 min-h-20 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                value={shortDescription}
                onChange={(e) => setShortDescription(e.target.value)}
                data-testid="admin-product-create-short"
              />
            </label>
            <div>
              <p className="mb-1 text-sm font-medium text-slate-700">توضیح کامل</p>
              <ProductRichTextEditor
                value={descriptionHtml}
                onChange={setDescriptionHtml}
                testId="admin-product-create-description"
              />
            </div>
          </div>
        ) : null}

        {step === "review" ? (
          <div className="space-y-4" data-testid="admin-product-create-panel-review">
            <h2 className="text-lg font-semibold text-slate-900">بررسی و ایجاد پیش‌نویس</h2>
            <dl className="grid gap-3 sm:grid-cols-2">
              <div className="rounded-xl border border-gray-100 bg-slate-50 p-3">
                <dt className="text-xs text-slate-500">دسته اصلی</dt>
                <dd className="mt-1 text-sm font-medium text-slate-800">{categoryPath || categoryId || "—"}</dd>
              </div>
              <div className="rounded-xl border border-gray-100 bg-slate-50 p-3">
                <dt className="text-xs text-slate-500">نام فارسی</dt>
                <dd className="mt-1 text-sm font-medium text-slate-800">{title || "—"}</dd>
              </div>
              <div className="rounded-xl border border-gray-100 bg-slate-50 p-3">
                <dt className="text-xs text-slate-500">نامک</dt>
                <dd className="mt-1 text-sm font-medium text-slate-800" dir="ltr">
                  {slug || "—"}
                </dd>
              </div>
              <div className="rounded-xl border border-gray-100 bg-slate-50 p-3">
                <dt className="text-xs text-slate-500">وضعیت</dt>
                <dd className="mt-1 text-sm font-medium text-amber-800">پیش‌نویس</dd>
              </div>
            </dl>
            <p className="text-sm text-slate-600">
              پس از ایجاد، ویژگی‌ها، تنوع‌ها، رسانه و SEO را در فضای کاری محصول تکمیل کنید.
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
                onClick={goNext}
                data-testid="admin-product-create-next"
              >
                ادامه
              </Button>
            ) : (
              <Button
                type="button"
                disabled={busy}
                onClick={() => void onCreate()}
                data-testid="admin-product-create-submit"
              >
                {busy ? "در حال ایجاد…" : "ایجاد پیش‌نویس"}
              </Button>
            )}
          </div>
        </div>
      </section>
    </main>
  );
}
