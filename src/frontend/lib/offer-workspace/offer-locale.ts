"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale } from "../i18n/locale-context.tsx";
import { mapSupportedLocale, type SupportedLocaleDefinition } from "../i18n/supported-locales.ts";
import { loadAdminLanguages } from "../../features/admin-languages/api/language-api.ts";
import {
  offerLocaleFromAppLocale,
  offerLocaleFromRegistry,
  type OfferListMessages,
  type OfferLocaleDefinition,
} from "./i18n.ts";
import { offerWorkspaceMessages, type OfferWorkspaceMessages } from "./messages.ts";
import { offerListMessagesRu, offerWorkspaceMessagesRu } from "./messages-ru.ts";

export interface OfferLocaleResolution {
  locale: OfferLocaleDefinition;
  workspaceMessages?: OfferWorkspaceMessages;
  listMessages?: OfferListMessages;
  requestedLocale: string | null;
  resolved: boolean;
}

async function loadPublicLanguages(): Promise<SupportedLocaleDefinition[]> {
  try {
    const response = await fetch("/v1/languages", { headers: { Accept: "application/json" } });
    if (!response.ok) return [];
    const rows = await response.json();
    return Array.isArray(rows)
      ? rows.map(mapSupportedLocale).filter((row): row is SupportedLocaleDefinition => row != null)
      : [];
  } catch {
    return [];
  }
}

function requestedLocaleCode(): string | null {
  if (typeof window === "undefined") return null;
  const params = new URLSearchParams(window.location.search);
  const raw = params.get("locale") ?? params.get("lang");
  return raw?.trim() || null;
}

/**
 * Offer-owned locale resolution from the DB language registry. The global admin shell remains on
 * its existing fa/en routing; an offer route may request any active registry locale with ?locale=.
 */
export function useOfferLocaleOverride(role: "admin" | "seller"): OfferLocaleResolution {
  const appLocale = offerLocaleFromAppLocale(useLocale());
  const requested = requestedLocaleCode();
  const [registryLocale, setRegistryLocale] = useState<OfferLocaleDefinition | null>(null);
  const [overrides, setOverrides] = useState<{ workspace?: OfferWorkspaceMessages; list?: OfferListMessages }>({});

  useEffect(() => {
    let active = true;
    if (!requested || requested.toLowerCase() === appLocale.code.toLowerCase() || requested.toLowerCase() === appLocale.language) {
      setRegistryLocale(null);
      setOverrides({});
      return () => { active = false; };
    }
    const load = role === "admin"
      ? loadAdminLanguages().then((result) => result.state === "ok" ? result.data ?? [] : [])
      : loadPublicLanguages();
    void load.then((rows) => {
      if (!active) return;
      const needle = requested.toLowerCase();
      const row = rows.find((item) =>
        item.active && (
          item.code.toLowerCase() === needle
          || item.code.toLowerCase().startsWith(`${needle}-`)
          || item.code.toLowerCase().split("-")[0] === needle
        ));
      if (!row) {
        setRegistryLocale(null);
        setOverrides({});
        return;
      }
      const locale = offerLocaleFromRegistry({
        code: row.code,
        culture: row.culture,
        direction: row.direction,
        calendarDisplay: row.calendarDisplay,
      });
      setRegistryLocale(locale);
      if (row.code.toLowerCase().startsWith("ru")) {
        setOverrides({ workspace: offerWorkspaceMessagesRu, list: offerListMessagesRu });
      } else {
        const base = offerWorkspaceMessages[locale.language as "fa" | "en"] ?? offerWorkspaceMessages.en;
        setOverrides({ workspace: base });
      }
    });
    return () => { active = false; };
  }, [appLocale.code, appLocale.language, requested, role]);

  return useMemo(() => ({
    locale: registryLocale ?? appLocale,
    workspaceMessages: overrides.workspace,
    listMessages: overrides.list,
    requestedLocale: requested,
    resolved: Boolean(registryLocale) || requested == null,
  }), [appLocale, overrides, registryLocale, requested]);
}
