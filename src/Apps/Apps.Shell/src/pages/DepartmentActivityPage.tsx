import type { NavModule } from "@platform/ui";
import type { AuthenticatedUser } from "../api/authApi";
import type { ActivityItem } from "../useDepartmentActivity";
import { useDepartmentActivity } from "../useDepartmentActivity";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";

interface DepartmentActivityPageProps {
  language: SupportedLanguageCode;
  module: NavModule;
  user: AuthenticatedUser;
}

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected" | "je.statusPosted" | "je.statusReversed"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
  Posted: "je.statusPosted",
  Reversed: "je.statusReversed",
};

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

function goTo(href: string) {
  window.location.hash = href;
}

function ActivityTable({ items, language, emptyMessage }: { items: ActivityItem[]; language: SupportedLanguageCode; emptyMessage: string }) {
  if (items.length === 0) return <p className="fin-report__panel-empty">{emptyMessage}</p>;
  return (
    <table className="pi-dense-table">
      <thead>
        <tr>
          <th>{t("activity.columnDocument", language)}</th>
          <th>{t("activity.columnType", language)}</th>
          <th>{t("activity.columnStatus", language)}</th>
          <th>{t("activity.columnDate", language)}</th>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.key} style={{ cursor: "pointer" }} onClick={() => goTo(item.href)}>
            <td><bdi dir="ltr">{item.label}</bdi></td>
            <td>{item.docTypeLabel}</td>
            <td>{translateStatus(item.status, language)}</td>
            <td><bdi dir="ltr">{new Date(item.createdAt).toLocaleDateString(language === "ar" ? "ar-SA" : "en-US")}</bdi></td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

/**
 * One department's real activity — "documents Submitted for approval that I hold the Approve role for" and
 * "documents I created" — reused across every real department (same "one component, every department"
 * precedent as DepartmentLandingPage.tsx). All data comes from useDepartmentActivity.ts; this component is
 * pure presentation.
 */
export function DepartmentActivityPage({ language, module, user }: DepartmentActivityPageProps) {
  const { approvals, submitted, isLoading } = useDepartmentActivity(module.key, user, language);

  return (
    <section className="fin-report-page">
      <h1>{module.label}</h1>
      <p className="fin-report__subtitle">{t("activity.subtitle", language)}</p>
      {isLoading && <p>{t("status.loading", language)}</p>}

      <div className="activity-page__section">
        <h2>{t("activity.tabApprovals", language)}</h2>
        <ActivityTable items={approvals} language={language} emptyMessage={t("activity.emptyApprovals", language)} />
      </div>

      <div className="activity-page__section">
        <h2>{t("activity.tabSubmitted", language)}</h2>
        <ActivityTable items={submitted} language={language} emptyMessage={t("activity.emptySubmitted", language)} />
      </div>
    </section>
  );
}
