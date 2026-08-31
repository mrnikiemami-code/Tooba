import { fromHostGridPage, toHostGridQuery } from "./grid-query-mapper.ts";
import type { GridServerQuery, GridServerPage } from "../data-grid/types.ts";

export type AdminGridQueryResult<TRow> = {
  source: "host" | "error";
  page: GridServerPage<TRow>;
  message?: string;
  denied?: boolean;
};

type AdminGridHeaders = Record<string, string>;

/** POST helper for module-owned Admin GridQuery endpoints. */
export async function postAdminGridQuery<TItem, TRow extends { id: string }>(
  path: string,
  query: GridServerQuery,
  headers: AdminGridHeaders,
  mapRow: (item: unknown) => TRow | null,
): Promise<AdminGridQueryResult<TRow>> {
  try {
    const response = await fetch(path, {
      method: "POST",
      headers: { ...headers, "Content-Type": "application/json" },
      body: JSON.stringify(toHostGridQuery(query)),
    });
    if (response.status === 401 || response.status === 403) {
      return {
        source: "error",
        page: { rows: [], total: 0 },
        message: "admin.authorization.denied",
        denied: true,
      };
    }
    if (!response.ok) {
      return {
        source: "error",
        page: { rows: [], total: 0 },
        message: `host-grid-http-${String(response.status)}`,
      };
    }
    const payload = (await response.json()) as {
      items?: unknown[];
      totalCount?: number;
      page?: number;
      pageSize?: number;
    };
    const page = fromHostGridPage(
      {
        items: payload.items ?? [],
        page: payload.page ?? query.page,
        pageSize: payload.pageSize ?? query.pageSize,
        totalCount: payload.totalCount ?? 0,
      },
      (item) => {
        const row = mapRow(item);
        if (!row) {
          throw new Error("admin-grid-row-map-failed");
        }
        return row;
      },
    );
    return { source: "host", page };
  } catch {
    return { source: "error", page: { rows: [], total: 0 }, message: "host-unreachable" };
  }
}
