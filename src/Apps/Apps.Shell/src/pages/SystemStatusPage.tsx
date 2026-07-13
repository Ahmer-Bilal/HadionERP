import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { fetchGreeting, fetchHealth, fetchSystemStatus } from "../api/systemApi";
import type { SystemGreeting, SystemStatus } from "../api/systemApi";

interface SystemStatusPageProps {
  language: SupportedLanguageCode;
}

export function SystemStatusPage({ language }: SystemStatusPageProps) {
  const [health, setHealth] = useState<string | null>(null);
  const [status, setStatus] = useState<SystemStatus | null>(null);
  const [greeting, setGreeting] = useState<SystemGreeting | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    setError(null);
    Promise.all([fetchHealth(), fetchSystemStatus(), fetchGreeting(language)])
      .then(([healthResult, statusResult, greetingResult]) => {
        setHealth(healthResult);
        setStatus(statusResult);
        setGreeting(greetingResult);
      })
      .catch(() => {
        setError(t("status.error", language));
      });
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  // The ActionPane's actions are driven by what's available — here just Refresh (re-fetches status). This
  // is the same stateless pattern a real record form will use: the page decides which actions apply based
  // on the document's lifecycle state + the user's security role, and passes only those to ActionPane.
  const actions: ActionItem[] = [
    { key: "refresh", label: t("status.actionRefresh", language), onClick: load, variant: "primary" },
  ];

  return (
    <section id="system-status" className="status-page">
      <h1>{t("status.heading", language)}</h1>

      <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />

      {error && <p className="status-page__error">{error}</p>}

      {!error && !status && <p>{t("status.loading", language)}</p>}

      {status && (
        <FastTabs
          tabs={[
            {
              key: "general",
              title: t("status.tabGeneral", language),
              defaultExpanded: true,
              content: (
                <dl className="status-page__facts">
                  <dt>{t("status.applicationLabel", language)}</dt>
                  <dd>{status.application}</dd>

                  <dt>{t("status.phaseLabel", language)}</dt>
                  <dd>{status.phase}</dd>

                  <dt>{t("status.kernelServicesLabel", language)}</dt>
                  <dd>{status.kernelServicesWired.join(", ")}</dd>

                  <dt>/health</dt>
                  <dd>{health}</dd>
                </dl>
              ),
            },
            {
              key: "events-audit",
              title: t("status.tabEventsAudit", language),
              defaultExpanded: true,
              content: (
                <dl className="status-page__facts">
                  <dt>{t("status.eventsOutboxLabel", language)}</dt>
                  {/* A bare "N / M" digit sequence is bidi-neutral — inside an Arabic (RTL) paragraph the
                      browser visually reorders it. <bdi dir="ltr"> isolates it so it always reads in the
                      intended published/pending order regardless of the surrounding text direction. */}
                  <dd>
                    <bdi dir="ltr">
                      {status.eventsOutbox.published} / {status.eventsOutbox.pending}
                    </bdi>
                  </dd>

                  <dt>{t("status.auditLabel", language)}</dt>
                  <dd>
                    <bdi dir="ltr">{status.audit.entries}</bdi>{" "}
                    {/* chainValid is a backend re-verification of the hash chain on every status read; a
                        broken chain would be a serious integrity signal, so it's surfaced plainly. */}
                    {status.audit.chainValid
                      ? `(${t("status.auditChainValid", language)})`
                      : `(${t("status.auditChainBroken", language)})`}
                  </dd>
                </dl>
              ),
            },
            {
              key: "localization",
              title: t("status.tabLocalization", language),
              content: greeting ? (
                <div className="status-page__greeting" dir={greeting.direction}>
                  <h2>{t("status.greetingHeading", language)}</h2>
                  <p>{greeting.message}</p>
                </div>
              ) : (
                <p>{t("status.loading", language)}</p>
              ),
            },
          ]}
        />
      )}
    </section>
  );
}
