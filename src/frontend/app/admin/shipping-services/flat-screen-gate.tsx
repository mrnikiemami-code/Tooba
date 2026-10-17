"use client";

/**
 * TB-SHIPPING-ADMIN-DEV-AUTH-VISIBILITY-147 — دروازهٔ کلاینتیِ صفحهٔ «سرویس ارسال» تک‌سطحی.
 *
 * چرا این کامپوننت وجود دارد: تصمیم دروازه در سمت سرور فقط وقتی «روشن» می‌شود که `page.tsx` یک
 * هویت مدیرِ معتبر داشته باشد. در محیط Development، هویت مدیر در مرورگر از همان مسیر مجازِ همهٔ
 * صفحه‌های ادمین تأمین می‌شود (`prepareAdminDevActor`) و برای Server Component قابل خواندن نیست؛
 * پس خواندن سرور `403 admin.authorization.denied` می‌گیرد و پیش‌تر همین باعث می‌شد صفحهٔ قدیمیِ
 * دوسطحی «انواع سرویس (سطح ۲)» نمایش داده شود.
 *
 * قواعد سخت این دروازه:
 *
 * - تنها ورودی «روشن‌شدن»، پاسخ واقعی سرور است: `gate=Enabled` و `ready=true` از همان مسیر
 *   canonical خواندن (`loadOneLevelCatalog`). هیچ پارامتر query، هیچ پرچم بیلد، هیچ طول فهرست و
 *   هیچ fallback hardcoded نمی‌تواند دروازه را روشن کند.
 * - `prepareAdminDevActor` همان seam موجود و مجاز محیط Development است؛ فقط «هویت» را از Host
 *   می‌گیرد و هیچ رمز/OTP/هدر تازه‌ای نمی‌سازد. هیچ actor ممتازی hardcode یا جعل نمی‌شود.
 * - در `401`/`403`/خطا/آماده‌نبودن/قطع ارتباط: وضعیت صریح و بومی (فارسی) نشان داده می‌شود؛
 *   «تلاش دوباره» فقط برای شکست گذرا پیشنهاد می‌شود و «دروازهٔ خاموش در این محیط» به‌عنوان
 *   شکست پیکربندی، بدون دکمهٔ بی‌اثر نمایش داده می‌شود. هیچ‌گاه به صفحهٔ قدیمیِ سطح ارائه‌دهنده
 *   سقوط نمی‌کند و هیچ فلشِ صفحهٔ قدیمی ندارد.
 * - وقتی خواندن سرور موفق بوده (`serverResolved`)، همان دادهٔ SSR بلافاصله رندر می‌شود تا ادمینِ
 *   لاگین‌کرده هیچ بارگذاری دوباره‌ای نبیند.
 */

import { useCallback, useEffect, useRef, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import { prepareAdminDevActor } from "../admin-api.ts";
import { loadOneLevelCatalog } from "./flat-method-catalog-api.ts";
import { ShippingServicesFlatScreen } from "./flat-screen.tsx";
import { presentShippingCatalogFailure } from "./shipping-catalog-gate-presentation.ts";
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

type GateState = "resolving" | "ok" | "denied" | "error";

/** پیام بومیِ وضعیتِ در حال بررسی؛ هیچ‌کدام به صفحهٔ قدیمی راه نمی‌دهند. */
const RESOLVING_FA = "در حال بررسی دسترسی و بارگذاری روش‌های ارسال…";

export function ShippingServicesFlatScreenGate({
  serverResolved,
  initialMethods,
  initialActiveLanguageId,
}: {
  serverResolved: boolean;
  initialMethods: readonly OneLevelMethodRow[];
  initialActiveLanguageId: string;
}) {
  const [state, setState] = useState<GateState>(serverResolved ? "ok" : "resolving");
  const [methods, setMethods] = useState<readonly OneLevelMethodRow[]>(initialMethods);
  const [activeLanguageId, setActiveLanguageId] = useState(initialActiveLanguageId);
  const [message, setMessage] = useState<string | undefined>(undefined);
  // هر تلاش یک نسل تازه می‌گیرد تا پاسخ کهنه هرگز وضعیت نسل جدید را بازنویسی نکند.
  const generation = useRef(0);

  const resolve = useCallback(async () => {
    const run = ++generation.current;
    setState("resolving");
    await prepareAdminDevActor();
    const result = await loadOneLevelCatalog();
    if (run !== generation.current) return;
    if (result.state === "ok" && result.data) {
      setMethods(result.data.methods);
      setActiveLanguageId(result.data.activeLanguageId);
      setMessage(undefined);
      setState("ok");
      return;
    }
    setMessage(result.message);
    setState(result.state === "denied" ? "denied" : "error");
  }, []);

  useEffect(() => {
    if (serverResolved) return;
    void resolve();
  }, [resolve, serverResolved]);

  if (state === "ok") {
    return (
      <ShippingServicesFlatScreen
        initialMethods={methods}
        activeLanguageId={activeLanguageId}
      />
    );
  }

  if (state === "denied") {
    const view = presentShippingCatalogFailure("admin.authorization.denied");
    return (
      <div data-testid="admin-shipping-services-denied">
        <ErrorState
          title={view.title}
          detail={view.detail}
          onRetry={view.retryable ? () => void resolve() : undefined}
          retryLabel={faWorkspaceMessages.retry}
        />
      </div>
    );
  }

  if (state === "error") {
    const view = presentShippingCatalogFailure(message);
    return (
      <div data-testid="admin-shipping-services-error">
        <ErrorState
          title={view.title}
          detail={view.detail}
          onRetry={view.retryable ? () => void resolve() : undefined}
          retryLabel={faWorkspaceMessages.retry}
        />
      </div>
    );
  }

  return (
    <p className="text-sm text-gray-500" data-testid="admin-shipping-services-resolving">
      {RESOLVING_FA}
    </p>
  );
}
