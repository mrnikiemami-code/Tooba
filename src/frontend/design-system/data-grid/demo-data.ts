import { createMemorySavedViewStore } from "./DataGrid";
import { executeGridQuery } from "./query-engine";
import type { EntityFilterAdapter, GridBulkAction, GridColumnDef, GridQueryAdapter } from "./types";

/**
 * ردیف نمایشی ویترین. موجودیت Product دامنه نیست.
 */
export interface DemoOpsRow {
  id: string;
  reference: string;
  seller: string;
  amount: number;
  currency: string;
  bookedOn: string;
  status: string;
  hold: boolean;
  quantity: number;
  channel: string;
}

export const demoOpsRows: DemoOpsRow[] = Array.from({ length: 47 }, (_, index) => ({
  id: `row-${index + 1}`,
  reference: `OPS-${String(1000 + index)}`,
  seller: index % 5 === 0 ? "seller-north" : "seller-south",
  amount: 120000 + index * 1500,
  currency: "IRR",
  bookedOn: `2026-04-${String((index % 27) + 1).padStart(2, "0")}`,
  status: index % 3 === 0 ? "pending" : index % 3 === 1 ? "open" : "closed",
  hold: index % 4 === 0,
  quantity: 1 + (index % 9),
  channel: index % 2 === 0 ? "web" : "app",
}));

export const demoOpsColumns: GridColumnDef<DemoOpsRow>[] = [
  {
    id: "reference",
    header: "Reference",
    accessor: (row) => row.reference,
    width: 140,
    minWidth: 96,
    maxWidth: 280,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "seller",
    header: "Seller",
    accessor: (row) => row.seller,
    width: 140,
    minWidth: 96,
    maxWidth: 240,
    filterKind: "entity",
  },
  {
    id: "amount",
    header: "Amount",
    accessor: (row) => row.amount,
    cell: (row) => `${row.amount} ${row.currency}`,
    width: 140,
    minWidth: 96,
    maxWidth: 200,
    filterKind: "money",
    align: "end",
  },
  {
    id: "bookedOn",
    header: "Booked",
    accessor: (row) => row.bookedOn,
    width: 120,
    minWidth: 96,
    maxWidth: 180,
    filterKind: "date",
  },
  {
    id: "status",
    header: "Status",
    accessor: (row) => row.status,
    width: 120,
    minWidth: 96,
    maxWidth: 180,
    filterKind: "status",
    enumOptions: [
      { value: "pending", label: "pending" },
      { value: "open", label: "open" },
      { value: "closed", label: "closed" },
    ],
  },
  {
    id: "hold",
    header: "Hold",
    accessor: (row) => row.hold,
    cell: (row) => (row.hold ? "yes" : "no"),
    width: 96,
    minWidth: 72,
    maxWidth: 120,
    filterKind: "boolean",
  },
  {
    id: "quantity",
    header: "Qty",
    accessor: (row) => row.quantity,
    width: 96,
    minWidth: 72,
    maxWidth: 140,
    filterKind: "number",
    align: "end",
  },
  {
    id: "channel",
    header: "Channel",
    accessor: (row) => row.channel,
    width: 110,
    minWidth: 80,
    maxWidth: 160,
    filterKind: "enum",
    enumOptions: [
      { value: "web", label: "web" },
      { value: "app", label: "app" },
    ],
  },
];

export const demoEntityLookup: EntityFilterAdapter = {
  async search(term: string) {
    const needle = term.trim().toLowerCase();
    const sellers = [...new Set(demoOpsRows.map((row) => row.seller))];
    return sellers
      .filter((seller) => !needle || seller.toLowerCase().includes(needle))
      .map((seller) => ({ id: seller, label: seller }));
  },
};

export const demoQueryAdapter: GridQueryAdapter<DemoOpsRow> = async (query) =>
  executeGridQuery(demoOpsRows, demoOpsColumns, query);

export const demoBulkActions: GridBulkAction<DemoOpsRow>[] = [
  {
    id: "tag",
    label: "Tag selected",
    requiresConfirmation: true,
    isAvailable: (rows) => rows.length > 0,
    execute: async (rows) => ({ ok: true, message: `tagged ${rows.length}` }),
  },
];

export const demoSavedViewStore = createMemorySavedViewStore();
