"use client";

import { useMemo, useState } from "react";
import {
  Button,
  Card,
  Cluster,
  DataGrid,
  enGridMessages,
  faGridMessages,
  Stack,
} from "../../design-system";
import type { GridQueryAdapter } from "../../design-system/data-grid";
import {
  demoBulkActions,
  demoEntityLookup,
  demoOpsColumns,
  demoQueryAdapter,
  demoSavedViewStore,
  type DemoOpsRow,
} from "../../design-system/data-grid/demo-data";

/**
 * ویترین گرید با دادهٔ ساختگی. به API دامنه وصل نیست.
 */
export function GridShowcase() {
  const [locale, setLocale] = useState<"fa" | "en">("fa");
  const [mode, setMode] = useState<"live" | "empty" | "error">("live");

  const queryAdapter = useMemo<GridQueryAdapter<DemoOpsRow>>(() => {
    if (mode === "error") {
      return async () => {
        throw new Error("demo error");
      };
    }
    if (mode === "empty") {
      return async () => ({ rows: [], total: 0 });
    }
    return demoQueryAdapter;
  }, [mode]);

  return (
    <Card>
      <Stack>
        <h2 className="ds-title">Professional Data Grid</h2>
        <p className="ds-caption text-muted">دادهٔ مصنوعی عملیاتی؛ workspace محصول/سفارش نیست.</p>
        <Cluster>
          <Button type="button" tone="secondary" onClick={() => setLocale((current) => (current === "fa" ? "en" : "fa"))}>
            {locale}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setMode("live")}>
            live
          </Button>
          <Button type="button" tone="secondary" onClick={() => setMode("empty")}>
            empty
          </Button>
          <Button type="button" tone="secondary" onClick={() => setMode("error")}>
            error
          </Button>
        </Cluster>
        <DataGrid
          key={`${locale}-${mode}`}
          columns={demoOpsColumns}
          queryAdapter={queryAdapter}
          messages={locale === "fa" ? faGridMessages : enGridMessages}
          bulkActions={demoBulkActions}
          savedViewStore={demoSavedViewStore}
          entityLookup={demoEntityLookup}
          onServerExport={async () => undefined}
        />
      </Stack>
    </Card>
  );
}
