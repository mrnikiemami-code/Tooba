"use client";

/**
 * AppCategoryTree — پوشش Tooba روی Ant Design Tree.
 * انواع/props خام Ant به لایهٔ صفحه نشت نمی‌کند.
 */

import { ConfigProvider, Tree } from "antd";
import {
  ChevronLeft,
  Folder,
  GripVertical,
  MoreVertical,
  Plus,
  Search,
  X,
} from "lucide-react";
/* GripVertical: دستگیرهٔ drag جدا از عنوان/شِورون */
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type MouseEvent,
  type ReactNode,
} from "react";
import { cn } from "../cn";
import {
  canAddCategoryChild,
  categoryStatusLabel,
  filterCategoryForest,
  isValidCategoryDrop,
  splitHighlight,
  type AppCategoryTreeNode,
  type CategoryDropPosition,
  type CategoryDropRequest,
} from "./tree-model";
import "./theme.css";

export type {
  AppCategoryTreeNode,
  CategoryDropPosition,
  CategoryDropRequest,
  CategoryNodeStatus,
} from "./tree-model";

export interface AppCategoryTreeProps {
  nodes: AppCategoryTreeNode[];
  expandedKeys: string[];
  selectedKeys: string[];
  onExpandedKeysChange: (keys: string[]) => void;
  onSelect: (id: string) => void;
  onDropRequest?: (request: CategoryDropRequest) => void | Promise<void>;
  searchQuery?: string;
  onSearchQueryChange?: (query: string) => void;
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  onCreateRoot?: () => void;
  onCreateChild?: (parentId: string) => void;
  direction?: "rtl" | "ltr";
  className?: string;
  /** ارتفاع ناحیهٔ مجازی؛ در صورت پشتیبانی امن Ant فعال می‌شود. */
  virtualHeight?: number;
  allowDrag?: boolean;
  title?: string;
  createLabel?: string;
  searchPlaceholder?: string;
  emptyTitle?: string;
  emptyCtaLabel?: string;
  noSearchResultsLabel?: string;
  uiLocale?: "fa" | "en";
}

type InternalTreeData = {
  key: string;
  title: ReactNode;
  children?: InternalTreeData[];
  isLeaf?: boolean;
  selectable?: boolean;
  className?: string;
};

function statusClass(status: AppCategoryTreeNode["status"]): string {
  if (status === "Published") return "app-category-tree-node__status--published";
  if (status === "Archived") return "app-category-tree-node__status--archived";
  return "app-category-tree-node__status--draft";
}

function CategoryNodeRow({
  node,
  expanded,
  matched,
  searchQuery,
  allowDrag,
  uiLocale,
  canCreateChild,
  onToggleExpand,
  onSelect,
  onCreateChild,
  onOpenMenu,
}: {
  node: AppCategoryTreeNode;
  expanded: boolean;
  matched: boolean;
  searchQuery: string;
  allowDrag: boolean;
  uiLocale: "fa" | "en";
  canCreateChild: boolean;
  onToggleExpand: (id: string) => void;
  onSelect: (id: string) => void;
  onCreateChild?: (parentId: string) => void;
  onOpenMenu: (id: string, anchor: HTMLElement) => void;
}) {
  const hasKids = Boolean(node.children?.length) || node.hasChildren;
  const parts = splitHighlight(node.name || "—", matched ? searchQuery : "");

  const onChevron = (e: MouseEvent | KeyboardEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!hasKids) return;
    onToggleExpand(node.id);
  };

  const onTitle = (e: MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    onSelect(node.id);
  };

  return (
    <div className="app-category-tree-node" data-testid={`category-tree-node-${node.id}`}>
      <button
        type="button"
        className="app-category-tree-node__chevron"
        aria-label={expanded ? "بستن زیرمجموعه‌ها" : "باز کردن زیرمجموعه‌ها"}
        aria-expanded={expanded}
        aria-disabled={!hasKids}
        disabled={!hasKids}
        onClick={onChevron}
        data-testid={`category-tree-chevron-${node.id}`}
      >
        <ChevronLeft className="app-category-tree-node__chevron-icon" size={16} aria-hidden />
      </button>

      <span className="app-category-tree-node__icon" aria-hidden>
        <Folder size={16} />
      </span>

      <button
        type="button"
        className="app-category-tree-node__title"
        onClick={onTitle}
        data-testid={`category-tree-title-${node.id}`}
      >
        <span className="app-category-tree-node__name">
          {parts.map((part, i) =>
            part.match ? (
              <mark key={i} className="app-category-tree-node__mark">
                {part.text}
              </mark>
            ) : (
              <span key={i}>{part.text}</span>
            ),
          )}
        </span>
        <span className={cn("app-category-tree-node__status", statusClass(node.status))}>
          {categoryStatusLabel(node.status, uiLocale)}
        </span>
      </button>

      <div className="app-category-tree-node__actions">
        {onCreateChild && canCreateChild ? (
          <button
            type="button"
            className="app-category-tree-node__icon-btn"
            aria-label="افزودن زیرمجموعه"
            title="افزودن زیرمجموعه"
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              onCreateChild(node.id);
            }}
            data-testid={`category-tree-add-child-${node.id}`}
          >
            <Plus size={16} aria-hidden />
          </button>
        ) : null}

        <button
          type="button"
          className="app-category-tree-node__icon-btn"
          aria-label="عملیات بیشتر"
          title="عملیات بیشتر"
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            onOpenMenu(node.id, e.currentTarget);
          }}
          data-testid={`category-tree-menu-${node.id}`}
        >
          <MoreVertical size={16} aria-hidden />
        </button>

        {/* دستگیرهٔ drag واقعی از طریق draggable.icon در Tree تزریق می‌شود */}
        {allowDrag ? (
          <span
            className="app-category-tree-node__icon-btn app-category-tree-node__drag"
            aria-hidden
            data-testid={`category-tree-drag-slot-${node.id}`}
          />
        ) : null}
      </div>
    </div>
  );
}

/**
 * درخت رده با کنترل کامل expand/select/search/drag توسط Tooba.
 */
export function AppCategoryTree({
  nodes,
  expandedKeys,
  selectedKeys,
  onExpandedKeysChange,
  onSelect,
  onDropRequest,
  searchQuery = "",
  onSearchQueryChange,
  loading = false,
  error = null,
  onRetry,
  onCreateRoot,
  onCreateChild,
  direction = "rtl",
  className,
  virtualHeight = 420,
  allowDrag = true,
  title = "دسته‌بندی‌ها",
  createLabel = "دسته‌بندی جدید",
  searchPlaceholder = "جستجوی دسته‌بندی…",
  emptyTitle = "هنوز دسته‌بندی‌ای ثبت نشده است",
  emptyCtaLabel = "اولین دسته‌بندی را ایجاد کنید",
  noSearchResultsLabel = "نتیجه‌ای برای این جستجو پیدا نشد",
  uiLocale = "fa",
}: AppCategoryTreeProps) {
  const [menu, setMenu] = useState<{ id: string; top: number; left: number } | null>(null);
  const wrapRef = useRef<HTMLDivElement | null>(null);
  const localSearch = onSearchQueryChange != null;
  const [internalQuery, setInternalQuery] = useState(searchQuery);
  const query = localSearch ? searchQuery : internalQuery;

  useEffect(() => {
    if (!localSearch) setInternalQuery(searchQuery);
  }, [localSearch, searchQuery]);

  useEffect(() => {
    if (!menu) return;
    const close = () => setMenu(null);
    window.addEventListener("click", close);
    return () => window.removeEventListener("click", close);
  }, [menu]);

  const searchResult = useMemo(() => filterCategoryForest(nodes, query), [nodes, query]);

  useEffect(() => {
    if (!query.trim() || searchResult.autoExpandKeys.length === 0) return;
    const merged = new Set([...expandedKeys, ...searchResult.autoExpandKeys]);
    if (merged.size !== expandedKeys.length) {
      onExpandedKeysChange([...merged]);
    }
    // فقط هنگام تغییر query اجداد را باز می‌کنیم
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query]);

  // گرهٔ انتخاب‌شده را در دید نگه می‌داریم (deep-link / refresh / back-forward).
  useEffect(() => {
    const selectedId = selectedKeys[0];
    if (!selectedId || !wrapRef.current) return;
    const frame = window.requestAnimationFrame(() => {
      const selected = wrapRef.current?.querySelector(".ant-tree-treenode-selected");
      if (selected && typeof selected.scrollIntoView === "function") {
        selected.scrollIntoView({ block: "nearest", inline: "nearest" });
      }
    });
    return () => window.cancelAnimationFrame(frame);
  }, [selectedKeys, expandedKeys, nodes, query]);

  const expandedSet = useMemo(() => new Set(expandedKeys), [expandedKeys]);

  const toggleExpand = useCallback(
    (id: string) => {
      if (expandedSet.has(id)) {
        onExpandedKeysChange(expandedKeys.filter((k) => k !== id));
      } else {
        onExpandedKeysChange([...expandedKeys, id]);
      }
    },
    [expandedKeys, expandedSet, onExpandedKeysChange],
  );

  const openMenu = useCallback((id: string, anchor: HTMLElement) => {
    const rect = anchor.getBoundingClientRect();
    setMenu({ id, top: rect.bottom + 4, left: rect.left });
  }, []);

  const mapNodes = useCallback(
    (list: AppCategoryTreeNode[]): InternalTreeData[] =>
      list.map((node) => ({
        key: node.id,
        isLeaf: !(node.children?.length || node.hasChildren),
        selectable: false,
        title: (
          <CategoryNodeRow
            node={node}
            expanded={expandedSet.has(node.id)}
            matched={searchResult.matchedIds.has(node.id)}
            searchQuery={query}
            allowDrag={allowDrag && Boolean(onDropRequest)}
            uiLocale={uiLocale}
            canCreateChild={canAddCategoryChild(nodes, node.id)}
            onToggleExpand={toggleExpand}
            onSelect={onSelect}
            onCreateChild={onCreateChild}
            onOpenMenu={openMenu}
          />
        ),
        children: node.children?.length ? mapNodes(node.children) : undefined,
      })),
    [
      allowDrag,
      expandedSet,
      nodes,
      onCreateChild,
      onDropRequest,
      onSelect,
      openMenu,
      query,
      searchResult.matchedIds,
      toggleExpand,
      uiLocale,
    ],
  );

  const treeData = useMemo(
    () => mapNodes(searchResult.filteredForest),
    [mapNodes, searchResult.filteredForest],
  );

  const setQuery = (value: string) => {
    if (onSearchQueryChange) onSearchQueryChange(value);
    else setInternalQuery(value);
  };

  const handleDrop = async (info: {
    dragNode: { key: string | number };
    node: { key: string | number };
    dropPosition: number;
    dropToGap: boolean;
  }) => {
    if (!onDropRequest) return;
    const dragId = String(info.dragNode.key);
    const dropId = String(info.node.key);
    let position: CategoryDropPosition;
    if (!info.dropToGap) {
      position = "inside";
    } else if (info.dropPosition === -1) {
      position = "before";
    } else {
      position = "after";
    }
    const request: CategoryDropRequest = { dragId, dropId, position };
    if (!isValidCategoryDrop(nodes, request)) {
      return;
    }
    await onDropRequest(request);
  };

  const showEmpty = !loading && !error && nodes.length === 0;
  const showNoSearch =
    !loading && !error && nodes.length > 0 && query.trim() && searchResult.filteredForest.length === 0;

  const treeStyle: CSSProperties = virtualHeight > 0 ? { height: virtualHeight } : {};

  return (
    <div
      ref={wrapRef}
      className={cn("app-category-tree", className)}
      dir={direction}
      data-testid="app-category-tree"
      data-direction={direction}
    >
      <div className="app-category-tree__toolbar">
        <div className="app-category-tree__title-row">
          <h2 className="app-category-tree__title">{title}</h2>
          {onCreateRoot ? (
            <button
              type="button"
              className="app-category-tree__create"
              onClick={onCreateRoot}
              data-testid="category-tree-create-root"
            >
              <Plus size={16} aria-hidden />
              {createLabel}
            </button>
          ) : null}
        </div>
        <div className="app-category-tree__search">
          <Search className="app-category-tree__search-icon" size={16} aria-hidden />
          <input
            className="app-category-tree__search-input"
            type="search"
            value={query}
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
            onChange={(e) => setQuery(e.target.value)}
            data-testid="category-tree-search"
          />
          {query ? (
            <button
              type="button"
              className="app-category-tree__search-clear"
              aria-label="پاک کردن جستجو"
              title="پاک کردن جستجو"
              onClick={() => setQuery("")}
              data-testid="category-tree-search-clear"
            >
              <X size={16} aria-hidden />
            </button>
          ) : null}
        </div>
      </div>

      <div className="app-category-tree__body">
        {loading ? (
          <div className="app-category-tree__state" data-testid="category-tree-loading">
            در حال بارگذاری درخت…
          </div>
        ) : null}

        {!loading && error ? (
          <div className="app-category-tree__state app-category-tree__state-error" data-testid="category-tree-error">
            <strong>بارگذاری ناموفق بود</strong>
            <span>{error}</span>
            {onRetry ? (
              <button type="button" className="app-category-tree__retry" onClick={onRetry}>
                تلاش دوباره
              </button>
            ) : null}
          </div>
        ) : null}

        {showEmpty ? (
          <div className="app-category-tree__state" data-testid="category-tree-empty">
            <strong>{emptyTitle}</strong>
            {onCreateRoot ? (
              <button
                type="button"
                className="app-category-tree__create"
                onClick={onCreateRoot}
                data-testid="category-tree-empty-cta"
              >
                {emptyCtaLabel}
              </button>
            ) : null}
          </div>
        ) : null}

        {showNoSearch ? (
          <div className="app-category-tree__state" data-testid="category-tree-no-search">
            {noSearchResultsLabel}
          </div>
        ) : null}

        {!loading && !error && !showEmpty && !showNoSearch ? (
          <ConfigProvider direction={direction}>
            <Tree
              className="app-category-tree__ant"
              treeData={treeData}
              expandedKeys={expandedKeys}
              selectedKeys={selectedKeys}
              onExpand={(keys) => onExpandedKeysChange(keys.map(String))}
              selectable={false}
              blockNode
              showIcon={false}
              draggable={
                allowDrag && onDropRequest
                  ? {
                      icon: (
                        <span
                          className="app-category-tree-node__icon-btn app-category-tree-node__drag"
                          aria-label="جابه‌جایی با کشیدن"
                          title="جابه‌جایی با کشیدن"
                          data-testid="category-tree-drag-handle"
                        >
                          <GripVertical size={16} aria-hidden />
                        </span>
                      ),
                      nodeDraggable: () => true,
                    }
                  : false
              }
              allowDrop={({ dropNode, dragNode, dropPosition }) => {
                const position: CategoryDropPosition =
                  dropPosition === 0 ? "inside" : dropPosition === -1 ? "before" : "after";
                return isValidCategoryDrop(nodes, {
                  dragId: String(dragNode.key),
                  dropId: String(dropNode.key),
                  position,
                });
              }}
              onDrop={(info) => {
                void handleDrop(info);
              }}
              height={virtualHeight > 0 ? virtualHeight : undefined}
              virtual={virtualHeight > 0}
              style={treeStyle}
            />
          </ConfigProvider>
        ) : null}
      </div>

      {menu ? (
        <div
          className="app-category-tree__menu"
          style={{ position: "fixed", top: menu.top, left: menu.left }}
          role="menu"
          data-testid="category-tree-actions-menu"
          onClick={(e) => e.stopPropagation()}
        >
          {onCreateChild && canAddCategoryChild(nodes, menu.id) ? (
            <button
              type="button"
              role="menuitem"
              data-testid="category-tree-menu-add-child"
              onClick={() => {
                onCreateChild(menu.id);
                setMenu(null);
              }}
            >
              افزودن زیرمجموعه
            </button>
          ) : null}
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              onSelect(menu.id);
              setMenu(null);
            }}
          >
            باز کردن
          </button>
        </div>
      ) : null}
    </div>
  );
}
