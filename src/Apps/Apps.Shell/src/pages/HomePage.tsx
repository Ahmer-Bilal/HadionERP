import { useEffect, useState } from "react";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { listBusinessPartners } from "../api/businessPartnerApi";
import { listGLAccounts } from "../api/glAccountApi";
import { listItems } from "../api/itemApi";

interface HomePageProps {
  language: SupportedLanguageCode;
}

interface TileData {
  key: string;
  titleKey: "bp.heading" | "gl.heading" | "item.heading";
  href: string;
  total: number;
  pendingApproval: number;
}

const PENDING_STATUS = "Submitted";

export function HomePage({ language }: HomePageProps) {
  const [tiles, setTiles] = useState<TileData[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const [partners, accounts, items] = await Promise.all([
          listBusinessPartners(200, 0),
          listGLAccounts(200, 0),
          listItems(200, 0),
        ]);
        if (cancelled) return;

        setTiles([
          {
            key: "business-partners",
            titleKey: "bp.heading",
            href: "#business-partners",
            total: partners.totalCount,
            pendingApproval: partners.items.filter((p) => p.status === PENDING_STATUS).length,
          },
          {
            key: "gl-accounts",
            titleKey: "gl.heading",
            href: "#gl-accounts",
            total: accounts.totalCount,
            pendingApproval: accounts.items.filter((a) => a.status === PENDING_STATUS).length,
          },
          {
            key: "items",
            titleKey: "item.heading",
            href: "#items",
            total: items.totalCount,
            pendingApproval: items.items.filter((i) => i.status === PENDING_STATUS).length,
          },
        ]);
      } catch {
        if (!cancelled) setError(t("status.error", language));
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [language]);

  const tileStyle: React.CSSProperties = {
    display: "flex",
    flexDirection: "column",
    gap: "0.5rem",
    padding: "1.25rem",
    minInlineSize: "14rem",
    border: "1px solid var(--pi-border, #d0d7de)",
    borderRadius: "0.5rem",
    cursor: "pointer",
    background: "var(--pi-surface, #fff)",
  };

  return (
    <section>
      <h1>{t("home.heading", language)}</h1>
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {!tiles && !error && <p>{t("status.loading", language)}</p>}
      {tiles && (
        <div style={{ display: "flex", flexWrap: "wrap", gap: "1rem" }}>
          {tiles.map((tile) => (
            <div
              key={tile.key}
              style={tileStyle}
              role="link"
              tabIndex={0}
              onClick={() => { window.location.hash = tile.href; }}
              onKeyDown={(e) => { if (e.key === "Enter") window.location.hash = tile.href; }}
            >
              <span style={{ fontWeight: 600 }}>{t(tile.titleKey, language)}</span>
              <span style={{ fontSize: "2rem", fontWeight: 700 }}>{tile.total}</span>
              <span style={{ color: "var(--pi-text-muted, #6e7781)" }}>{t("home.totalLabel", language)}</span>
              {tile.pendingApproval > 0 && (
                <span style={{ color: "var(--pi-warning, #9a6700)", fontWeight: 600 }}>
                  {tile.pendingApproval} {t("home.pendingApprovalLabel", language)}
                </span>
              )}
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
