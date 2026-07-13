import { useState } from "react";
import type { FastTabItem } from "../types";

/**
 * FastTabs — vertically stacked, collapsible panels where several can be open at once
 * (docs/architecture/02-business-object-model.md #2.1: "FastTabs, not tabs: several can be expanded
 * simultaneously, the form scrolls vertically through them — this is a deliberate Microsoft UX change
 * replacing the older fixed horizontal tab strip, and we inherit it rather than reinventing a tab strip").
 *
 * Each tab header is a real <button> with aria-expanded/aria-controls for keyboard + screen-reader access
 * (WCAG 2.1 AA, per doc 02 #4). Internal state tracks which tabs are open; opening one does not close
 * another — that's the defining difference from a tab strip.
 */
interface FastTabsProps {
  tabs: FastTabItem[];
}

export function FastTabs({ tabs }: FastTabsProps) {
  // Start with the tabs marked defaultExpanded open; the rest closed.
  const [expanded, setExpanded] = useState<Set<string>>(
    () => new Set(tabs.filter((tab) => tab.defaultExpanded).map((tab) => tab.key)),
  );

  const toggle = (key: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  return (
    <div className="pi-fast-tabs">
      {tabs.map((tab) => {
        const panelId = `pi-fast-tab__panel-${tab.key}`;
        const isOpen = expanded.has(tab.key);
        return (
          <section key={tab.key} className={"pi-fast-tab" + (isOpen ? " is-open" : "")}>
            <button
              type="button"
              className="pi-fast-tab__header"
              aria-expanded={isOpen}
              aria-controls={panelId}
              onClick={() => toggle(tab.key)}
            >
              <span className="pi-fast-tab__chevron" aria-hidden="true" />
              <span className="pi-fast-tab__title">{tab.title}</span>
            </button>
            {isOpen && (
              <div id={panelId} className="pi-fast-tab__content">
                {tab.content}
              </div>
            )}
          </section>
        );
      })}
    </div>
  );
}
