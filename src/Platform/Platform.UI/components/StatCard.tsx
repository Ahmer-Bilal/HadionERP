import type { CSSProperties, ReactNode } from "react";

/**
 * A KPI tile — icon, label, value, optional trend — the building block every mockup dashboard/report screen
 * (UI/Finance/*.png) opens with as a stat-card strip. Pure presentation, same "receives already-formatted
 * strings, never calls a translation function itself" rule as every other Platform.UI component
 * (see index.ts's own doc comment) — a consumer passes an already-localized/-formatted value string
 * (e.g. "SAR 12.4M"), never a raw number this component would have to format itself.
 */
export type StatCardTrendDirection = "up" | "down" | "neutral";

interface StatCardTrend {
  label: string;
  direction: StatCardTrendDirection;
}

interface StatCardProps {
  label: string;
  value: string;
  icon?: ReactNode;
  /** The icon badge's color (any valid CSS color, typically one of the --pi-chart-N, --pi-success,
   * --pi-danger, or --pi-warning tokens) — defaults to --pi-accent when omitted. Distinct tones across a
   * KPI row make each card visually findable at a glance instead of reading as one undifferentiated color. */
  tone?: string;
  trend?: StatCardTrend;
}

export function StatCard({ label, value, icon, tone, trend }: StatCardProps) {
  return (
    <div className="pi-stat-card">
      {icon && (
        <span className="pi-stat-card__icon" style={tone ? ({ "--pi-stat-card-tone": tone } as CSSProperties) : undefined}>
          {icon}
        </span>
      )}
      <div className="pi-stat-card__body">
        <span className="pi-stat-card__label">{label}</span>
        <span className="pi-stat-card__value"><bdi dir="ltr">{value}</bdi></span>
        {trend && (
          <span className={`pi-stat-card__trend pi-stat-card__trend--${trend.direction}`}>{trend.label}</span>
        )}
      </div>
    </div>
  );
}
