"use client";

import { useEffect, useMemo, useState } from "react";
import { Check, X } from "lucide-react";
import { SearchableSelect, type SearchableSelectOption } from "../../../design-system/primitives/searchable-select";
import styles from "../../../lib/offer-workspace/offer-workspace.module.css";
import pv from "./shipping-preview.module.css";
import { loadActiveRegistryLanguages, resolveRegistryLanguage, type RegistryLocaleDefinition } from "../../../lib/i18n/registry-locale-model";

type Locale = "fa" | "en" | "ru";
type Mode = "auto" | "specific";

const copy = {
  fa: {
    dir: "rtl", title: "ارسال", desc: "مسئولیت ارسال، نوع ارسال و روش‌های مجاز این آفر را تعیین کنید.",
    responsibility: "مسئولیت ارسال", unassigned: "تعیین‌نشده", seller: "فروشنده", platform: "پلتفرم",
    type: "نوع ارسال", choose: "انتخاب کنید", mode: "حالت انتخاب روش ارسال",
    auto: "خودکار", autoHint: "تمام روش‌های مجازِ همین نوع ارسال", specific: "اختصاصی", specificHint: "فقط روش‌هایی که انتخاب می‌کنید",
    methods: "روش‌های ارسال", addMethod: "افزودن روش ارسال", search: "جست‌وجوی نام یا کد روش",
    noResults: "روشی با این جست‌وجو پیدا نشد", none: "هنوز روشی انتخاب نشده است.", remove: "حذف",
    locked: "در حالت خودکار، سامانه همه روش‌های مجاز را اعمال می‌کند.", note: "روش‌های غیرفعال، نامعتبر یا متعلق به فروشنده دیگر در فهرست نمایش داده نمی‌شوند.",
    preview: "پیش‌نمایش طراحی — بدون ذخیره‌سازی", locale: "زبان",
  },
  en: {
    dir: "ltr", title: "Shipping", desc: "Set the fulfilment responsibility, shipping type and allowed methods for this offer.",
    responsibility: "Fulfilment responsibility", unassigned: "Unassigned", seller: "Seller", platform: "Platform",
    type: "Shipping type", choose: "Choose", mode: "Method selection mode",
    auto: "Automatic", autoHint: "All allowed methods for this type", specific: "Specific", specificHint: "Only the methods you select",
    methods: "Shipping methods", addMethod: "Add shipping method", search: "Search method name or code",
    noResults: "No method matches this search", none: "No method selected yet.", remove: "Remove",
    locked: "Automatic mode applies every allowed method.", note: "Inactive, invalid or other-seller methods are not listed.",
    preview: "Design preview — not persisted", locale: "Language",
  },
  ru: {
    dir: "ltr", title: "Доставка", desc: "Укажите ответственность, тип доставки и разрешённые способы для этого предложения.",
    responsibility: "Ответственность за доставку", unassigned: "Не задано", seller: "Продавец", platform: "Платформа",
    type: "Тип доставки", choose: "Выберите", mode: "Режим выбора способа",
    auto: "Автоматически", autoHint: "Все разрешённые способы этого типа", specific: "Конкретные", specificHint: "Только выбранные способы",
    methods: "Способы доставки", addMethod: "Добавить способ", search: "Поиск по названию или коду",
    noResults: "Способ не найден", none: "Способ пока не выбран.", remove: "Удалить",
    locked: "Автоматический режим применяет все разрешённые способы.", note: "Неактивные, недопустимые или чужие способы не отображаются.",
    preview: "Предпросмотр дизайна — без сохранения", locale: "Язык",
  },
} as const;

// Isolated design fixtures; never persisted or used as operational method identities.
const shippingTypes = [
  { value: "normal", label: { fa: "عادی", en: "Standard", ru: "Обычная" } },
  { value: "plus", label: { fa: "پلاس", en: "Plus", ru: "Плюс" } },
  { value: "heavy", label: { fa: "سنگین", en: "Heavy", ru: "Тяжёлая" } },
  { value: "extra-heavy", label: { fa: "فوق سنگین", en: "Extra heavy", ru: "Сверхтяжёлая" } },
  { value: "grocery", label: { fa: "سوپرمارکتی", en: "Grocery", ru: "Продукты" } },
];
const methods = [
  { value: "truck", type: "heavy", owner: "Platform", code: "TRUCK", label: { fa: "ارسال با کامیون", en: "Truck", ru: "Грузовик" } },
  { value: "light-truck", type: "heavy", owner: "Platform", code: "LIGHT-TRUCK", label: { fa: "ارسال با خاور", en: "Light truck", ru: "Малый грузовик" } },
  { value: "freight", type: "heavy", owner: "Platform", code: "FREIGHT", label: { fa: "ارسال باربری", en: "Freight", ru: "Грузоперевозка" } },
  { value: "dedicated", type: "heavy", owner: "Platform", code: "DEDICATED", label: { fa: "ارسال اختصاصی", en: "Dedicated", ru: "Выделенная доставка" } },
  { value: "scheduled", type: "heavy", owner: "Platform", code: "SCHEDULED", label: { fa: "ارسال زمان‌بندی‌شده", en: "Scheduled", ru: "По расписанию" } },
  { value: "seller-truck", type: "heavy", owner: "Seller", code: "SELLER-TRUCK", label: { fa: "کامیون فروشنده", en: "Seller truck", ru: "Грузовик продавца" } },
  { value: "extra-truck", type: "extra-heavy", owner: "Platform", code: "EXTRA-TRUCK", label: { fa: "کامیون فوق سنگین", en: "Extra heavy truck", ru: "Сверхтяжёлый грузовик" } },
  { value: "post", type: "normal", owner: "Platform", code: "POST", label: { fa: "پست پیشتاز", en: "Express post", ru: "Экспресс-почта" } },
  { value: "plus-delivery", type: "plus", owner: "Platform", code: "PLUS", label: { fa: "ارسال پلاس", en: "Plus delivery", ru: "Доставка плюс" } },
  { value: "grocery-delivery", type: "grocery", owner: "Platform", code: "GROCERY", label: { fa: "ارسال فوری سوپرمارکتی", en: "Grocery delivery", ru: "Доставка продуктов" } },
];

export default function OfferShippingDesignPreview() {
  const [languages, setLanguages] = useState<RegistryLocaleDefinition[]>([]);
  const [requested, setRequested] = useState<string | null>(null);
  const [registryError, setRegistryError] = useState(false);
  useEffect(() => {
    let live = true;
    const prefix = window.location.pathname.split("/").filter(Boolean)[0];
    setRequested(prefix === "design" ? null : prefix);
    void loadActiveRegistryLanguages().then(rows => { if (live) setLanguages(rows); })
      .catch(() => { if (live) setRegistryError(true); });
    return () => { live = false; };
  }, []);
  const definition = resolveRegistryLanguage(languages, requested);
  const locale: Locale = definition?.language === "ru" ? "ru" : definition?.language === "fa" || (!definition && requested !== "en" && requested !== "ru") ? "fa" : "en";
  const t = copy[locale];
  const [responsibility, setResponsibility] = useState<"Seller" | "Platform" | "">("Platform");
  const [type, setType] = useState("heavy");
  const [mode, setMode] = useState<Mode>("specific");
  const [selected, setSelected] = useState<string[]>([]);
  const ready = Boolean(responsibility && type);
  const options = useMemo(() => methods.filter(m => m.type === type && m.owner === responsibility), [type, responsibility]);
  const selectedRows = options.filter(m => selected.includes(m.value));
  const addOptions: SearchableSelectOption[] = options
    .filter(m => !selected.includes(m.value))
    .map(m => ({ value: m.value, label: m.label[locale], searchText: `${m.label.fa} ${m.label.en} ${m.label.ru} ${m.code}` }));
  const emptySelected = ready && mode === "specific" && !selectedRows.length;
  const validation = locale === "fa" ? "حداقل یک روش ارسال انتخاب کنید." : locale === "ru" ? "Выберите хотя бы один способ." : "Select at least one shipping method.";
  const noEligible = locale === "fa" ? "روش ارسال مجازی وجود ندارد." : locale === "ru" ? "Нет доступных способов." : "No eligible shipping methods.";
  const registryUnavailable = locale === "fa" ? "فهرست زبان‌ها در دسترس نیست." : locale === "ru" ? "Языки недоступны." : "Language registry unavailable.";

  return (
    <div className={styles.root} dir={definition?.direction ?? t.dir} data-testid="shipping-design-preview">
      <header className={styles.stickyHead}>
        <div className={pv.headRow}>
          <div className={pv.headTexts}>
            <span className={styles.pill + " " + styles.pillNeutral}>{t.preview}</span>
            <h1 className={styles.headName}>{t.title}</h1>
          </div>
          <div className={pv.actions}>
            <span className={styles.resultMeta}>{t.locale}</span>
            {languages.map(row => (
              <button key={row.code} type="button" className={`${styles.btn} ${styles.btnSm} ${row.code === definition?.code ? styles.btnPrimary : styles.btnQuiet}`} onClick={() => setRequested(row.urlPrefix)}>{row.nativeName}</button>
            ))}
            {registryError ? <span role="status">{registryUnavailable}</span> : null}
          </div>
        </div>
      </header>
      <main className={styles.page}>
        <div className={styles.stack} data-testid="offer-tab-shipping">
          <section className={`${styles.card} ${pv.previewCard}`}>
            <header className={styles.cardHd}><h2 className={pv.cardTitle}>{t.title}</h2></header>
            <div className={styles.cardBd}>
              <div className={styles.formGrid}>
                <label className={styles.field}><span className={styles.fieldLabel}>{t.responsibility}</span>
                  <select className={`${styles.ctl} ${styles.select}`} value={responsibility} onChange={e => { setResponsibility(e.target.value as typeof responsibility); setType(""); setSelected([]); }} data-testid="preview-responsibility">
                    <option value="">{t.unassigned}</option><option value="Seller">{t.seller}</option><option value="Platform">{t.platform}</option>
                  </select>
                </label>
                <label className={styles.field}><span className={styles.fieldLabel}>{t.type}</span>
                  <select className={`${styles.ctl} ${styles.select}`} value={type} onChange={e => { setType(e.target.value); setSelected([]); }} disabled={!responsibility} data-testid="preview-type">
                    <option value="">{t.choose}</option>
                    {shippingTypes.map(x => <option key={x.value} value={x.value}>{x.label[locale]}</option>)}
                  </select>
                </label>
              </div>

              <div className={styles.field} style={{ marginTop: 16 }}>
                <span className={styles.fieldLabel}>{t.mode}</span>
                <div className={pv.segmented} data-testid="preview-mode">
                  <button type="button" className={mode === "auto" ? pv.segActive : pv.seg} disabled={!ready} aria-pressed={mode === "auto"} onClick={() => { setMode("auto"); setSelected([]); }}>
                    {mode === "auto" ? <Check size={14} aria-hidden /> : null}<span><strong>{t.auto}</strong><small>{t.autoHint}</small></span>
                  </button>
                  <button type="button" className={mode === "specific" ? pv.segActive : pv.seg} disabled={!ready} aria-pressed={mode === "specific"} onClick={() => setMode("specific")}>
                    {mode === "specific" ? <Check size={14} aria-hidden /> : null}<span><strong>{t.specific}</strong><small>{t.specificHint}</small></span>
                  </button>
                </div>
              </div>

              {!ready || mode === "auto" ? null : (
                <div className={styles.field} style={{ marginTop: 14 }}>
                  <span className={styles.fieldLabel}>{t.methods}</span>
                  <div className={pv.chips} data-testid="preview-selected-methods">
                    {selectedRows.map(m => (
                      <span key={m.value} className={styles.chip}>{m.label[locale]} <span className={pv.chipCode} dir="ltr">{m.code}</span>
                        <button type="button" aria-label={`${t.remove} ${m.label[locale]}`} className={pv.chipRemove} onClick={() => setSelected(list => list.filter(x => x !== m.value))} data-testid={`preview-remove-${m.value}`}><X size={12} aria-hidden /></button>
                      </span>
                    ))}
                  </div>
                  <div className={pv.methodPicker}>
                    <SearchableSelect options={addOptions} value="" onChange={v => setSelected(list => options.some(row => row.value === v) && !list.includes(v) ? [...list, v] : list)} label={t.addMethod} placeholder={t.addMethod} searchLabel={t.search} emptyLabel={t.noResults} disabled={!addOptions.length} testId="preview-method-picker" />
                  </div>
                  {!options.length ? <p className={styles.fieldHint} role="status">{noEligible}</p> : emptySelected ? <p className={styles.fieldErr} role="status">{validation}</p> : null}
                </div>
              )}
            </div>
          </section>
        </div>
      </main>
    </div>
  );
}
