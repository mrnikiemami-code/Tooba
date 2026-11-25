"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Button, Input } from "../../design-system";
import type { OfferWorkspaceMessages } from "./messages";
import type {
  OfferMode,
  OfferRole,
  OfferSellerOption,
  OfferVariantOption,
  OfferWorkspaceDetail,
  OfferPickerPage,
} from "./types";
import { FieldRow, Note, TabHeading } from "./tab-shared";

interface PickerState<T> {
  rows: T[];
  page: number;
  total: number;
  hasMore: boolean;
  loading: boolean;
  error: string | null;
}

function emptyPicker<T>(): PickerState<T> {
  return { rows: [], page: 1, total: 0, hasMore: false, loading: false, error: null };
}

export interface IdentityTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  mode: OfferMode;
  role: OfferRole;
  detail: OfferWorkspaceDetail | null;
  sellerSku: string;
  onSellerSkuChange: (value: string) => void;
  fixedSeller?: OfferSellerOption;
  selectedSeller: OfferSellerOption | null;
  selectedVariant: OfferVariantOption | null;
  onSelectSeller: (seller: OfferSellerOption) => void;
  onSelectVariant: (variant: OfferVariantOption) => void;
  searchSellers?: (search: string, page: number) => Promise<OfferPickerPage<OfferSellerOption>>;
  searchVariants: (search: string, page: number) => Promise<OfferPickerPage<OfferVariantOption>>;
  extra?: ReactNode;
}

export function IdentityTab(props: IdentityTabProps) {
  const { t, mode, detail } = props;
  if (mode === "edit" && detail) {
    return (
      <div data-testid="offer-tab-identity">
        <TabHeading title={t.identityTab} hint={t.lockedIdentity} />
        <div className="grid gap-3 sm:grid-cols-2">
          <FieldRow label={t.offerId} value={<span dir="ltr" className="font-mono text-xs">{detail.offerId}</span>} />
          <FieldRow label={t.seller} value={`${detail.sellerDisplayName || detail.sellerPartyId} · ${t.lockedSeller}`} />
          <FieldRow label={t.product} value={detail.productTitle} />
          <FieldRow label={t.variant} value={<span dir="ltr" className="font-mono text-xs">{detail.catalogVariantId}</span>} />
          <FieldRow label={t.channel} value={detail.channel === "Marketplace" ? t.channelMarketplace : detail.channel} />
        </div>
        <div className="mt-4 grid gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            {t.sellerSku}
            <Input
              value={props.sellerSku}
              onChange={(event) => props.onSellerSkuChange(event.target.value)}
              dir="ltr"
              data-testid="offer-seller-sku"
            />
          </label>
          <Note>{t.channelImmutable}</Note>
        </div>
        {props.extra ? <div className="mt-5">{props.extra}</div> : null}
      </div>
    );
  }

  return (
    <div data-testid="offer-tab-identity">
      <TabHeading title={t.identityTab} hint={t.dependentDisabled} />
      {props.fixedSeller ? (
        <div className="mb-4 rounded-ds border border-border bg-secondary/40 px-3 py-2 text-sm">
          <span className="text-muted">{t.seller}: </span>
          <span className="font-medium" data-testid="offer-fixed-seller">
            {props.fixedSeller.displayName || t.sellerSession}
          </span>
        </div>
      ) : (
        <SellerPicker {...props} />
      )}
      <VariantPicker {...props} />
      <label className="mt-4 flex max-w-md flex-col gap-1 text-sm">
        {t.sellerSku}
        <Input
          value={props.sellerSku}
          onChange={(event) => props.onSellerSkuChange(event.target.value)}
          dir="ltr"
          data-testid="offer-seller-sku"
        />
      </label>
      <div className="mt-4">
        <Note>{t.channelImmutable}</Note>
      </div>
      {props.extra ? <div className="mt-5">{props.extra}</div> : null}
    </div>
  );
}

function SellerPicker(props: IdentityTabProps) {
  const { t, searchSellers } = props;
  const [query, setQuery] = useState("");
  const [state, setState] = useState<PickerState<OfferSellerOption>>(emptyPicker);

  async function load(search: string, page: number) {
    if (!searchSellers) return;
    setState((current) => ({ ...current, loading: true, error: null }));
    try {
      const result = await searchSellers(search, page);
      setState({
        rows: result.rows,
        page: result.page,
        total: result.totalCount,
        hasMore: result.hasMore,
        loading: false,
        error: null,
      });
    } catch {
      setState((current) => ({ ...current, loading: false, error: t.pickerError }));
    }
  }

  useEffect(() => {
    void load("", 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <section className="mb-4" data-testid="offer-seller-picker">
      <h4 className="mb-2 text-sm font-semibold">{t.selectSeller}</h4>
      <div className="flex flex-wrap gap-2">
        <Input
          className="min-w-48 flex-1"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              void load(query, 1);
            }
          }}
          placeholder={t.search}
          data-testid="offer-seller-search"
        />
        <Button type="button" tone="secondary" onClick={() => void load(query, 1)}>
          {t.search}
        </Button>
      </div>
      <div className="mt-2 max-h-56 overflow-y-auto rounded-ds border border-border">
        {state.loading ? <p className="p-3 text-xs text-muted">{t.loading}</p> : null}
        {state.error ? <p className="p-3 text-xs text-danger">{state.error}</p> : null}
        {!state.loading && !state.error && state.rows.length === 0 ? (
          <p className="p-3 text-xs text-muted">{t.noResults}</p>
        ) : null}
        {state.rows.map((seller) => {
          const active = props.selectedSeller?.sellerPartyId === seller.sellerPartyId;
          return (
            <button
              key={seller.sellerPartyId}
              type="button"
              aria-pressed={active}
              onClick={() => props.onSelectSeller(seller)}
              className={`flex min-h-11 w-full items-center justify-between px-3 text-start text-sm ${
                active ? "bg-primary/10 font-semibold" : "hover:bg-secondary/60"
              }`}
              data-testid="offer-seller-option"
            >
              <span>{seller.displayName || seller.sellerPartyId}</span>
              {active ? <span className="text-xs text-primary">✓</span> : null}
            </button>
          );
        })}
      </div>
      <PickerPager t={t} state={state} onPage={(page) => void load(query, page)} />
    </section>
  );
}

function VariantPicker(props: IdentityTabProps) {
  const { t, searchVariants } = props;
  const [query, setQuery] = useState("");
  const [state, setState] = useState<PickerState<OfferVariantOption>>(emptyPicker);

  async function load(search: string, page: number) {
    setState((current) => ({ ...current, loading: true, error: null }));
    try {
      const result = await searchVariants(search, page);
      setState({
        rows: result.rows,
        page: result.page,
        total: result.totalCount,
        hasMore: result.hasMore,
        loading: false,
        error: null,
      });
    } catch {
      setState((current) => ({ ...current, loading: false, error: t.pickerError }));
    }
  }

  useEffect(() => {
    void load("", 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <section data-testid="offer-variant-picker">
      <h4 className="mb-2 text-sm font-semibold">{t.selectProduct}</h4>
      <div className="flex flex-wrap gap-2">
        <Input
          className="min-w-48 flex-1"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              void load(query, 1);
            }
          }}
          placeholder={t.search}
          data-testid="offer-variant-search"
        />
        <Button type="button" tone="secondary" onClick={() => void load(query, 1)}>
          {t.search}
        </Button>
      </div>
      <div className="mt-2 max-h-56 overflow-y-auto rounded-ds border border-border">
        {state.loading ? <p className="p-3 text-xs text-muted">{t.loading}</p> : null}
        {state.error ? <p className="p-3 text-xs text-danger">{state.error}</p> : null}
        {!state.loading && !state.error && state.rows.length === 0 ? (
          <p className="p-3 text-xs text-muted">{t.noResults}</p>
        ) : null}
        {state.rows.map((variant) => {
          const active = props.selectedVariant?.catalogVariantId === variant.catalogVariantId;
          return (
            <button
              key={variant.catalogVariantId}
              type="button"
              aria-pressed={active}
              onClick={() => props.onSelectVariant(variant)}
              className={`flex min-h-11 w-full flex-col items-start px-3 py-2 text-start text-sm ${
                active ? "bg-primary/10 font-semibold" : "hover:bg-secondary/60"
              }`}
              data-testid="offer-variant-option"
            >
              <span>{variant.productTitle}</span>
              <span className="text-xs text-muted" dir="ltr">
                {variant.catalogCode ?? variant.catalogVariantId} · {variant.productStatus}
              </span>
            </button>
          );
        })}
      </div>
      <PickerPager t={t} state={state} onPage={(page) => void load(query, page)} />
    </section>
  );
}

function PickerPager<T>({
  t,
  state,
  onPage,
}: {
  t: OfferWorkspaceMessages;
  state: PickerState<T>;
  onPage: (page: number) => void;
}) {
  return (
    <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs text-muted">
      <span data-testid="offer-picker-page">
        {t.pageInfo} {state.page} · {state.total.toLocaleString("fa-IR")}
      </span>
      <span className="flex gap-2">
        <Button
          type="button"
          tone="ghost"
          disabled={state.page <= 1 || state.loading}
          onClick={() => onPage(state.page - 1)}
          data-testid="offer-picker-prev"
        >
          {t.prevPage}
        </Button>
        <Button
          type="button"
          tone="ghost"
          disabled={!state.hasMore || state.loading}
          onClick={() => onPage(state.page + 1)}
          data-testid="offer-picker-next"
        >
          {t.nextPage}
        </Button>
      </span>
    </div>
  );
}
