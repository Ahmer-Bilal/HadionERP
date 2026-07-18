import { DepartmentIcon } from "@platform/ui";
import type { DepartmentIconKey } from "@platform/ui";
import { t } from "../i18n/content";
import type { SupportedLanguageCode } from "../i18n/language";

interface ComingSoonPageProps {
  language: SupportedLanguageCode;
  icon: DepartmentIconKey;
  title: string;
}

/**
 * The honest stand-in for a department that's on the roadmap (ROADMAP.md's Checkpoint/Phase 4) but not
 * built yet — Inventory, HR & Payroll, Equipment, CRM all render this rather than either a dead nav link or
 * a page pretending to have real data. One shared component, not four near-duplicate page files, since the
 * only thing that varies per department is its icon and title.
 */
export function ComingSoonPage({ language, icon, title }: ComingSoonPageProps) {
  return (
    <section className="coming-soon-page">
      <span className="coming-soon-page__icon">
        <DepartmentIcon icon={icon} size={40} />
      </span>
      <h1>{title}</h1>
      <p>{t("nav.comingSoonMessage", language)}</p>
    </section>
  );
}
