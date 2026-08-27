"use client";

import { useCallback, useEffect, useState } from "react";
import { Mail, MapPin, Phone, Save, Settings, Store } from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import {
  loadSellerSettings,
  readSellerPartyId,
  saveSellerSettings,
  type HostReadSource,
  type SellerSettings,
} from "../seller-api";

/**
 * تنظیمات فروشگاه — فرم Shopeiva-derived فقط تب store؛ بدون toggle جعلی.
 */
export default function VendorSettingsPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [settings, setSettings] = useState<SellerSettings | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [busy, setBusy] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [displayName, setDisplayName] = useState("");
  const [legalName, setLegalName] = useState("");
  const [description, setDescription] = useState("");
  const [supportPhone, setSupportPhone] = useState("");
  const [supportEmail, setSupportEmail] = useState("");
  const [addressLine, setAddressLine] = useState("");

  function applySettings(next: SellerSettings) {
    setSettings(next);
    setDisplayName(next.displayName);
    setLegalName(next.legalName ?? "");
    setDescription(next.description ?? "");
    setSupportPhone(next.supportPhone ?? "");
    setSupportEmail(next.supportEmail ?? "");
    setAddressLine(next.addressLine ?? "");
  }

  const refresh = useCallback(() => {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    setSource("loading");
    void loadSellerSettings(sellerPartyId).then((result) => {
      setSource(result.source);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
      if (result.settings) {
        applySettings(result.settings);
      } else {
        setSettings(null);
      }
    });
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  async function onSave() {
    if (!settings?.canManage) return;
    const sellerPartyId = settings.sellerPartyId || readSellerPartyId(window.location.search);
    if (!sellerPartyId) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await saveSellerSettings(sellerPartyId, {
      displayName: displayName.trim(),
      legalName: legalName.trim() || null,
      description: description.trim() || null,
      supportPhone: supportPhone.trim() || null,
      supportEmail: supportEmail.trim() || null,
      addressLine: addressLine.trim() || null,
    });
    setBusy(false);
    if (!result.ok) {
      if (result.denied) {
        setDenied(true);
        return;
      }
      setError(result.errorCode === "host-unreachable" ? "ارتباط با Host برقرار نشد." : "ذخیرهٔ تنظیمات انجام نشد.");
      return;
    }
    applySettings(result.settings);
    setSuccess("تنظیمات فروشگاه ذخیره شد.");
    const reloaded = await loadSellerSettings(sellerPartyId);
    if (reloaded.settings) {
      applySettings(reloaded.settings);
    }
  }

  if (denied) {
    return (
      <main data-testid="vendor-settings-page">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهده یا مدیریت تنظیمات این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  const readOnly = settings ? !settings.canManage : true;

  return (
    <main className="space-y-6" data-testid="vendor-settings-page">
      <div className="max-w-3xl mx-auto">
        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden shadow-sm">
          <div className="p-4 md:p-6 border-b border-gray-200 bg-gradient-to-r from-[#2563EB]/5 to-transparent">
            <div className="flex items-center gap-2">
              <Settings className="w-5 h-5 text-[#2563EB]" />
              <h1 className="text-lg font-bold text-gray-900">تنظیمات فروشنده</h1>
            </div>
            <p className="text-sm text-gray-500 mt-1">پروفایل عملیاتی فروشگاه از Host</p>
          </div>

          <div className="flex overflow-x-auto border-b border-gray-200 scrollbar-hide">
            <button
              type="button"
              className="flex items-center gap-2 px-4 py-3 text-sm font-medium whitespace-nowrap border-b-2 border-[#2563EB] text-[#2563EB]"
              data-testid="vendor-settings-tab-store"
            >
              <Store className="w-4 h-4" />
              فروشگاه
            </button>
          </div>

          <div className="p-4 md:p-6">
            {source === "error" ? (
              <ErrorState
                title="Host در دسترس نیست"
                detail={message}
                onRetry={refresh}
                retryLabel={faWorkspaceMessages.retry}
              />
            ) : source === "loading" ? (
              <p className="text-sm text-gray-500" data-testid="vendor-settings-loading">
                در حال دریافت تنظیمات فروشگاه...
              </p>
            ) : (
              <form
                className="space-y-4"
                onSubmit={(event) => {
                  event.preventDefault();
                  void onSave();
                }}
                data-testid="vendor-settings-store-form"
              >
                {readOnly ? (
                  <p
                    className="rounded-xl bg-amber-50 border border-amber-100 text-amber-800 text-sm px-4 py-3"
                    data-testid="vendor-settings-readonly"
                  >
                    شما فقط مجوز مشاهده دارید؛ ذخیرهٔ تنظیمات برای این Actor فعال نیست.
                  </p>
                ) : null}

                <div>
                  <label className="text-sm font-medium text-gray-700">نام فروشگاه</label>
                  <input
                    type="text"
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    disabled={readOnly || busy}
                    className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                    data-testid="vendor-settings-display-name"
                  />
                </div>

                <div>
                  <label className="text-sm font-medium text-gray-700">نام حقوقی (اختیاری)</label>
                  <input
                    type="text"
                    value={legalName}
                    onChange={(e) => setLegalName(e.target.value)}
                    disabled={readOnly || busy}
                    className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                    data-testid="vendor-settings-legal-name"
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
                      <Phone className="w-3.5 h-3.5 text-gray-400" />
                      شماره تماس
                    </label>
                    <input
                      type="text"
                      value={supportPhone}
                      onChange={(e) => setSupportPhone(e.target.value)}
                      disabled={readOnly || busy}
                      className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                      data-testid="vendor-settings-support-phone"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
                      <Mail className="w-3.5 h-3.5 text-gray-400" />
                      ایمیل
                    </label>
                    <input
                      type="email"
                      value={supportEmail}
                      onChange={(e) => setSupportEmail(e.target.value)}
                      disabled={readOnly || busy}
                      className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                      data-testid="vendor-settings-support-email"
                    />
                  </div>
                </div>

                <div>
                  <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-gray-400" />
                    آدرس
                  </label>
                  <textarea
                    rows={2}
                    value={addressLine}
                    onChange={(e) => setAddressLine(e.target.value)}
                    disabled={readOnly || busy}
                    className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none disabled:opacity-60"
                    data-testid="vendor-settings-address-line"
                  />
                </div>

                <div>
                  <label className="text-sm font-medium text-gray-700">توضیح فروشگاه (اختیاری)</label>
                  <textarea
                    rows={3}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    disabled={readOnly || busy}
                    className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none disabled:opacity-60"
                    data-testid="vendor-settings-description"
                  />
                </div>

                {!readOnly ? (
                  <button
                    type="submit"
                    disabled={busy || !displayName.trim()}
                    className="w-full py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-[#1D4ED8] transition-colors shadow-lg shadow-[#2563EB]/30 flex items-center justify-center gap-2 disabled:opacity-70 disabled:cursor-not-allowed"
                    data-testid="vendor-settings-save"
                  >
                    {busy ? (
                      <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    ) : (
                      <>
                        <Save className="w-4 h-4" />
                        ذخیره تنظیمات
                      </>
                    )}
                  </button>
                ) : null}

                {success ? (
                  <p className="text-xs font-bold text-emerald-600" data-testid="vendor-settings-success">
                    {success}
                  </p>
                ) : null}
                {error ? (
                  <p className="text-xs font-bold text-red-600" data-testid="vendor-settings-error">
                    {error}
                  </p>
                ) : null}
              </form>
            )}
          </div>
        </div>
      </div>
    </main>
  );
}
