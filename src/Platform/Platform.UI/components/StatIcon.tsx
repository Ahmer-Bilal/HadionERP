/**
 * A small generic icon set for StatCard's icon slot — "what kind of figure is this" (a count of things, a
 * currency total, a status), not a department identity (that's DepartmentIcon's job) or page chrome (that's
 * ShellBar's own local icon set). Same hand-drawn stroke style as every other icon in this design system:
 * viewBox 0 0 20 20, stroke=currentColor, so each one picks up StatCard's per-card tone automatically.
 */
export type StatIconKey =
  | "users" | "checkCircle" | "layers" | "leaf" | "power"
  | "coins" | "trendingUp" | "trendingDown" | "scale" | "receipt";

interface StatIconProps {
  icon: StatIconKey;
  size?: number;
}

export function StatIcon({ icon, size = 18 }: StatIconProps) {
  const common = {
    width: size, height: size, viewBox: "0 0 20 20", fill: "none", stroke: "currentColor",
    strokeWidth: 1.5, strokeLinecap: "round" as const, strokeLinejoin: "round" as const, "aria-hidden": true,
  };
  switch (icon) {
    case "users":
      return (
        <svg {...common}>
          <circle cx="7.2" cy="7" r="2.4" />
          <path d="M2.6 16c0-2.4 2-4.2 4.6-4.2s4.6 1.8 4.6 4.2" />
          <circle cx="14" cy="7.4" r="2" />
          <path d="M12 12.2c1.9.4 3.4 1.8 3.4 3.8" />
        </svg>
      );
    case "checkCircle":
      return (
        <svg {...common}>
          <circle cx="10" cy="10" r="7.3" />
          <path d="M6.8 10.2 8.8 12l4.4-4.6" />
        </svg>
      );
    case "layers":
      return (
        <svg {...common}>
          <path d="M10 3.2 17 7l-7 3.8L3 7Z" />
          <path d="M3 10.6 10 14.4l7-3.8" />
          <path d="M3 14 10 17.8 17 14" />
        </svg>
      );
    case "leaf":
      return (
        <svg {...common}>
          <path d="M15.5 4.5c.6 6-3 11-9.5 11-1 0-2 0-2-1 0-6.5 5-10 11.5-10Z" />
          <path d="M6 15.5 15 6.5" />
        </svg>
      );
    case "power":
      return (
        <svg {...common}>
          <path d="M10 3v6.2" />
          <path d="M6 5.3a6.2 6.2 0 1 0 8 0" />
        </svg>
      );
    case "coins":
      return (
        <svg {...common}>
          <ellipse cx="7.3" cy="6.3" rx="4.3" ry="2.3" />
          <path d="M3 6.3v4.4c0 1.3 1.9 2.3 4.3 2.3s4.3-1 4.3-2.3V6.3" />
          <ellipse cx="12.7" cy="11.3" rx="4.3" ry="2.2" />
          <path d="M8.4 11.3v2.4c0 1.2 1.9 2.2 4.3 2.2s4.3-1 4.3-2.2v-2.4" />
        </svg>
      );
    case "trendingUp":
      return (
        <svg {...common}>
          <path d="M3 13.5 8 8.5l3 3 6-6.5" />
          <path d="M13.5 5h3.5v3.5" />
        </svg>
      );
    case "trendingDown":
      return (
        <svg {...common}>
          <path d="M3 6.5 8 11.5l3-3 6 6.5" />
          <path d="M13.5 15h3.5v-3.5" />
        </svg>
      );
    case "scale":
      return (
        <svg {...common}>
          <path d="M10 3v14M6 17h8" />
          <path d="M4 6h5M11 6h5" />
          <path d="M2.5 10 4.5 6l2 4Z" />
          <path d="M13.5 10l2-4 2 4Z" />
        </svg>
      );
    case "receipt":
      return (
        <svg {...common}>
          <path d="M5 3h10v14l-2-1.3L11 17l-2-1.3L7 17l-2-1.3V3Z" />
          <path d="M7.5 7h5M7.5 10h5" />
        </svg>
      );
  }
}
