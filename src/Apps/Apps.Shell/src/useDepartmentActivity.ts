import { useEffect, useState } from "react";
import { DEPARTMENT_ACTIVITY } from "./departmentActivity";
import type { AuthenticatedUser } from "./api/authApi";
import { t } from "./i18n/content";
import type { SupportedLanguageCode } from "./i18n/language";

export interface ActivityItem {
  key: string;
  /** The document's own number (e.g. "FIN-JE-2026-000017") — falls back to its id on the rare item with no
   * number assigned yet. */
  label: string;
  docTypeLabel: string;
  /** The document type's list page — not a deep link to this specific record, since none of these pages
   * support routing straight to one id yet (same limitation Home page's own "Recent Activity" panel already
   * has, and the same reason its links land on the list too). */
  href: string;
  status: string;
  createdAt: string;
}

interface DepartmentActivityResult {
  approvals: ActivityItem[];
  submitted: ActivityItem[];
  isLoading: boolean;
}

/**
 * Real, per-department Approvals ("documents Submitted for approval that I actually hold the Approve role
 * for") and Submitted ("documents I created, whatever their current status") — computed entirely on data
 * already available client-side (AuthenticatedUser.roleKeys + username from api/authApi.ts) against the
 * exact same list endpoints every department's own pages already call
 * (see departmentActivity.ts's own doc comment). No backend change, no new endpoint.
 *
 * Fetches once per `moduleKey` change, not on every render — the same department a user is browsing rarely
 * changes render-to-render, and refetching a department's whole document set on every keystroke elsewhere
 * on the page would be wasteful.
 */
export function useDepartmentActivity(
  moduleKey: string | null,
  user: AuthenticatedUser | null,
  language: SupportedLanguageCode,
): DepartmentActivityResult {
  const [approvals, setApprovals] = useState<ActivityItem[]>([]);
  const [submitted, setSubmitted] = useState<ActivityItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const docTypes = moduleKey ? DEPARTMENT_ACTIVITY[moduleKey] : undefined;
    if (!user || !docTypes || docTypes.length === 0) {
      setApprovals([]);
      setSubmitted([]);
      return;
    }

    let cancelled = false;
    setIsLoading(true);

    Promise.all(
      docTypes.map((docType) =>
        docType
          .list(200, 0)
          .then((page) => ({ docType, items: page.items }))
          .catch(() => ({ docType, items: [] })),
      ),
    )
      .then((results) => {
        if (cancelled) return;
        const nextApprovals: ActivityItem[] = [];
        const nextSubmitted: ActivityItem[] = [];

        for (const { docType, items } of results) {
          const canApprove = user.roleKeys.includes(docType.approverRoleKey);
          const docTypeLabel = t(docType.labelKey, language);
          for (const item of items) {
            const activityItem: ActivityItem = {
              key: `${docType.href}-${item.id}`,
              label: item.documentNumber ?? item.id,
              docTypeLabel,
              href: docType.href,
              status: item.status,
              createdAt: item.createdAt,
            };
            if (canApprove && item.status === "Submitted") nextApprovals.push(activityItem);
            if (item.createdBy === user.username) nextSubmitted.push(activityItem);
          }
        }

        const byNewest = (a: ActivityItem, b: ActivityItem) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        nextApprovals.sort(byNewest);
        nextSubmitted.sort(byNewest);
        setApprovals(nextApprovals);
        setSubmitted(nextSubmitted);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [moduleKey, user, language]);

  return { approvals, submitted, isLoading };
}
