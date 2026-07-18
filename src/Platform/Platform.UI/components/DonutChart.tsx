/**
 * A small donut chart with a legend — inline SVG, no charting library (matching this design system's
 * zero-dependency approach, same reasoning as DepartmentIcon's own hand-drawn SVGs). Used for the
 * category/composition breakdowns the mockup report screens show (e.g. Trial Balance's "Balance by Account
 * Category"). Pure presentation: every label/value arrives already formatted — see StatCard's own doc
 * comment for the same rule.
 */
export interface DonutChartSegment {
  key: string;
  label: string;
  /** Already-formatted display value (e.g. "SAR 46.2M (50.2%)") — this component only uses the raw
   * `value` field below to size the arc, never to format text itself. */
  displayValue: string;
  /** The raw magnitude used to size this segment's arc, relative to the other segments' magnitudes. Signed
   * values are accepted (e.g. a net loss) — the chart sizes every arc off `Math.abs(value)`, since a
   * negative arc length has no visual meaning; the sign still reaches the caller-formatted
   * {@link displayValue} text. */
  value: number;
  color: string;
}

interface DonutChartProps {
  segments: DonutChartSegment[];
  ariaLabel: string;
  size?: number;
  thickness?: number;
  centerLabel?: string;
  centerValue?: string;
}

export function DonutChart({ segments, ariaLabel, size = 120, thickness = 18, centerLabel, centerValue }: DonutChartProps) {
  const total = segments.reduce((sum, s) => sum + Math.abs(s.value), 0);
  const radius = (size - thickness) / 2;
  const circumference = 2 * Math.PI * radius;

  let offset = 0;
  const arcs = total > 0
    ? segments.filter((s) => Math.abs(s.value) > 0).map((s) => {
        const dash = (Math.abs(s.value) / total) * circumference;
        const arc = { ...s, dash, offset };
        offset += dash;
        return arc;
      })
    : [];

  return (
    <div className="pi-donut-chart">
      <div className="pi-donut-chart__graphic">
        <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} role="img" aria-label={ariaLabel}>
          <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke="var(--pi-border)" strokeWidth={thickness} />
          {arcs.map((arc) => (
            <circle
              key={arc.key}
              cx={size / 2}
              cy={size / 2}
              r={radius}
              fill="none"
              stroke={arc.color}
              strokeWidth={thickness}
              strokeDasharray={`${arc.dash} ${circumference - arc.dash}`}
              strokeDashoffset={-arc.offset}
              transform={`rotate(-90 ${size / 2} ${size / 2})`}
              strokeLinecap={arcs.length > 1 ? "butt" : "round"}
            />
          ))}
        </svg>
        {(centerLabel || centerValue) && (
          <div className="pi-donut-chart__center">
            {centerValue && <span className="pi-donut-chart__center-value"><bdi dir="ltr">{centerValue}</bdi></span>}
            {centerLabel && <span className="pi-donut-chart__center-label">{centerLabel}</span>}
          </div>
        )}
      </div>
      <ul className="pi-donut-chart__legend">
        {segments.map((s) => (
          <li key={s.key}>
            <span className="pi-donut-chart__swatch" style={{ background: s.color }} aria-hidden="true" />
            <span className="pi-donut-chart__legend-label">{s.label}</span>
            <span className="pi-donut-chart__legend-value"><bdi dir="ltr">{s.displayValue}</bdi></span>
          </li>
        ))}
      </ul>
    </div>
  );
}
