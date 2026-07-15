import type { ReactNode } from "react";

/**
 * SplitView — the list pane stays visible on one side while the detail pane slides in beside it, rather
 * than replacing the list (a full navigate/page-swap) the way both Dynamics 365 and SAP Fiori handle
 * drill-down. Closer to a mail client's master-detail than a traditional ERP record form: a user triaging
 * many records (e.g. a project manager checking ten purchase orders in a row) never loses their place in
 * the list. This is a deliberate departure from the reference products, not an oversight — see
 * project_visual_identity_decisions memory for the full reasoning.
 *
 * The detail pane's entrance is a CSS `@keyframes` animation (components.css) keyed by `detailKey` — when
 * the key changes, React unmounts the old detail content and mounts a fresh DOM node, which replays the
 * animation automatically (no animation library needed). The slide direction is logical, not physical: it
 * reads `--pi-slide-distance`, which design-tokens.css flips under `[dir="rtl"]`, so the panel always slides
 * in from the same *logical* side regardless of language.
 */
interface SplitViewProps {
  list: ReactNode;
  detail: ReactNode | null;
  /** Identifies which record is currently shown in the detail pane — pass the record's id. Changing this
   * value is what re-triggers the entrance animation for a newly selected record. */
  detailKey: string;
  /** Shown in the detail pane when nothing is selected yet. */
  emptyDetailHint: string;
  ariaLabel: string;
}

export function SplitView({ list, detail, detailKey, emptyDetailHint, ariaLabel }: SplitViewProps) {
  return (
    <div className="pi-split-view" aria-label={ariaLabel}>
      <div className="pi-split-view__list">{list}</div>
      <div className="pi-split-view__detail">
        {detail ? (
          <div key={detailKey} className="pi-split-view__detail-panel">
            {detail}
          </div>
        ) : (
          <div className="pi-split-view__detail-empty">{emptyDetailHint}</div>
        )}
      </div>
    </div>
  );
}
