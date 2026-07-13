import { useEffect, useState } from "react";
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

  useEffect(() => {
    let cancelled = false;

    setError(null);

    Promise.all([fetchHealth(), fetchSystemStatus(), fetchGreeting(language)])
      .then(([healthResult, statusResult, greetingResult]) => {
        if (cancelled) return;
        setHealth(healthResult);
        setStatus(statusResult);
        setGreeting(greetingResult);
      })
      .catch(() => {
        if (cancelled) return;
        setError(t("status.error", language));
      });

    return () => {
      cancelled = true;
    };
  }, [language]);

  return (
    <section id="system-status" className="status-page">
      <h1>{t("status.heading", language)}</h1>

      {error && <p className="status-page__error">{error}</p>}

      {!error && !status && <p>{t("status.loading", language)}</p>}

      {status && (
        <dl className="status-page__facts">
          <dt>{t("status.applicationLabel", language)}</dt>
          <dd>{status.application}</dd>

          <dt>{t("status.phaseLabel", language)}</dt>
          <dd>{status.phase}</dd>

          <dt>{t("status.kernelServicesLabel", language)}</dt>
          <dd>{status.kernelServicesWired.join(", ")}</dd>

          <dt>{t("status.eventsOutboxLabel", language)}</dt>
          {/* A bare "N / M" digit sequence is bidi-neutral — inside an Arabic (RTL) paragraph the
              browser visually reorders it, showing "0 / 1" for published=1, pending=0. <bdi dir="ltr">
              isolates it so it always reads in the intended published/pending order regardless of the
              surrounding text direction. */}
          <dd><bdi dir="ltr">{status.eventsOutbox.published} / {status.eventsOutbox.pending}</bdi></dd>

          <dt>/health</dt>
          <dd>{health}</dd>
        </dl>
      )}

      {greeting && (
        <div className="status-page__greeting" dir={greeting.direction}>
          <h2>{t("status.greetingHeading", language)}</h2>
          <p>{greeting.message}</p>
        </div>
      )}
    </section>
  );
}
