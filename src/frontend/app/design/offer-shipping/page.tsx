"use client";

import { useMemo, useState } from "react";
import { Check, Plus, X } from "lucide-react";
import { SearchableSelect, type SearchableSelectOption } from "../../../design-system/primitives/searchable-select";
import styles from "../../../lib/offer-workspace/offer-workspace.module.css";
import pv from "./shipping-preview.module.css";

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

const shippingTypes = [
  { value: "t-post", label: { fa: "پست", en: "Post", ru: "Почта" } },
  { value: "t-courier", label: { fa: "پیک", en: "Courier", ru: "Курьер" } },
  { value: "t-freight", label: { fa: "باربری", en: "Freight", ru: "Грузоперевозка" } },
];

const methods = [
  { value: "m-post-express", type: "t-post", code: "POST-EXP", label: { fa: "پست پیشتاز", en: "Post Express", ru: "Экспресс-почта" } },
  { value: "m-post-ordinary", type: "t-post", code: "POST-ORD", label: { fa: "پست عادی", en: "Ordinary Post", ru: "Обычная почта" } },
  { value: "m-tipax", type: "t-post", code: "TIPAX", label: { fa: "تیپاکس", en: "Tipax", ru: "Tipax" } },
  { value: "m-city-courier", type: "t-courier", code: "CITY-BIKE", label: { fa: "پیک درون‌شهری", en: "City courier", ru: "Городской курьер" } },
  { value: "m-freight", type: "t-freight", code: "FREIGHT", label: { fa: "باربری سراسری", en: "Nationwide freight", ru: "Междугородняя доставка" } },
];

export default function OfferShippingDesignPreview() {
  const [locale, setLocale] = useState<Locale>("fa");
  const t = copy[locale];
  const [responsibility, setResponsibility] = useState<"Seller" | "Platform" | "">("Platform");
  const [type, setType] = useState("t-post");
  const [mode, setMode] = useState<Mode>("specific");
  const [selected, setSelected] = useState<string[]>(["m-post-express", "m-tipax"]);
  const options = useMemo(() => methods.filter(m => m.type === type), [type]);
  const selectedRows = selected.map(id => methods.find(m => m.value === id)).filter(Boolean) as typeof methods;
  const addOptions: SearchableSelectOption[] = options
    .filter(m => !selected.includes(m.value))
    .map(m => ({ value: m.value, label: `${locale === "fa" ? m.label.fa : locale === "en" ? m.label.en : m.label.ru} — ${m.code}`, searchText: `${m.label.fa} ${m.label.en} ${m.code}` }));
  const typeLabel = shippingTypes.find(x => x.value === type)?.label[locale] ?? "";

  return (
    <div className={styles.root} dir={t.dir} data-testid="shipping-design-preview">
      <header className={styles.stickyHead}>
        <div className={pv.headRow}>
          <div className={pv.headTexts}>
            <span className={styles.pill + " " + styles.pillNeutral}>{t.preview}</span>
            <h1 className={styles.headName}>{t.title}</h1>
          </div>
          <div className={pv.actions}>
            <span className={styles.resultMeta}>{t.locale}</span>
            {(["fa", "en", "ru"] as Locale[]).map(code => (
              <button key={code} type="button" className={`${styles.btn} ${styles.btnSm} ${code === locale ? styles.btnPrimary : styles.btnQuiet}`} onClick={() => setLocale(code)}>{code.toUpperCase()}</button>
            ))}
          </div>
        </div>
      </header>
      <main className={styles.page}>
        <div className={styles.stack} data-testid="offer-tab-shipping">
          <section className={styles.card}>
            <header className={styles.cardHd}><h2 className={pv.cardTitle}>{t.title}</h2><p className={pv.cardDesc}>{t.desc}</p></header>
            <div className={styles.cardBd}>
              <div className={styles.formGrid}>
                <label className={styles.field}><span className={styles.fieldLabel}>{t.responsibility}</span>
                  <select className={`${styles.ctl} ${styles.select}`} value={responsibility} onChange={e => setResponsibility(e.target.value as typeof responsibility)} data-testid="preview-responsibility">
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
                  <button type="button" className={mode === "auto" ? pv.segActive : pv.seg} onClick={() => setMode("auto")}>
                    {mode === "auto" ? <Check size={14} aria-hidden /> : null}<span><strong>{t.auto}</strong><small>{t.autoHint}</small></span>
                  </button>
                  <button type="button" className={mode === "specific" ? pv.segActive : pv.seg} onClick={() => setMode("specific")}>
                    {mode === "specific" ? <Check size={14} aria-hidden /> : null}<span><strong>{t.specific}</strong><small>{t.specificHint}</small></span>
                  </button>
                </div>
              </div>

              {mode === "auto" ? <p className={styles.resultMeta} data-testid="preview-auto-note">{t.locked} ({t.choose}: {typeLabel})</p> : (
                <div className={styles.field} style={{ marginTop: 14 }}>
                  <span className={styles.fieldLabel}>{t.methods}</span>
                  <div className={pv.chips} data-testid="preview-selected-methods">
                    {selectedRows.length === 0 ? <span className={styles.resultMeta}>{t.none}</span> : selectedRows.map(m => (
                      <span key={m.value} className={styles.chip}>{m.label[locale]} <span className={pv.chipCode} dir="ltr">{m.code}</span>
                        <button type="button" aria-label={`${t.remove} ${m.label[locale]}`} className={pv.chipRemove} onClick={() => setSelected(list => list.filter(x => x !== m.value))} data-testid={`preview-remove-${m.value}`}><X size={12} aria-hidden /></button>
                      </span>
                    ))}
                  </div>
                  <div className={pv.methodPicker}>
                    <SearchableSelect options={addOptions} value="" onChange={v => setSelected(list => [...list, v])} label={t.addMethod} placeholder={t.addMethod} searchLabel={t.search} emptyLabel={t.noResults} disabled={!addOptions.length} testId="preview-method-picker" />
                    <Plus size={15} aria-hidden className={pv.pickerPlus} />
                  </div>
                  <p className={styles.fieldHint}>{t.note}</p>
                </div>
              )}
            </div>
          </section>
        </div>
      </main>
    </div>
  );
}
