import type { ReactNode } from "react";

/**
 * One consistent icon per department, per docs/architecture/09-navigation-and-ui-standard.md — used in
 * both the NavigationPane's module header and the landing page's department card, the same icon in both
 * places, never a different one per screen. Simple line-art SVGs (no external icon library, matching this
 * design system's zero-dependency approach): stroke=currentColor so each icon inherits whatever text color
 * its surrounding element sets, and no embedded text, so there is nothing here for the no-hardcoded-Arabic
 * check to ever flag.
 */
export type DepartmentIconKey =
  | "home"
  | "platform-administration"
  | "master-data"
  | "finance"
  | "procurement"
  | "project-management"
  | "construction"
  | "inventory"
  | "hr-payroll"
  | "equipment"
  | "crm";

interface DepartmentIconProps {
  icon: DepartmentIconKey;
  size?: number;
}

const PATHS: Record<DepartmentIconKey, ReactNode> = {
  home: (
    <>
      <path d="M3 10.5 10 4l7 6.5" />
      <path d="M5 9.5v7a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-7" />
      <path d="M8 17.5v-4h4v4" />
    </>
  ),
  "platform-administration": (
    <>
      <path d="M10 3 16 5.2v4.3c0 4-2.6 7-6 7.5-3.4-.5-6-3.5-6-7.5V5.2L10 3Z" />
      <path d="M7.5 10.2 9.3 12l3.2-3.6" />
    </>
  ),
  "master-data": (
    <>
      <ellipse cx="10" cy="5.5" rx="6" ry="2.2" />
      <path d="M4 5.5v4c0 1.2 2.7 2.2 6 2.2s6-1 6-2.2v-4" />
      <path d="M4 9.5v4c0 1.2 2.7 2.2 6 2.2s6-1 6-2.2v-4" />
    </>
  ),
  finance: (
    <>
      <path d="M4 4.5h9a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H4Z" />
      <path d="M4 4.5v13" />
      <path d="M7 8h6M7 11h6M7 14h4" />
    </>
  ),
  procurement: (
    <>
      <path d="M10 3 17 6.5 10 10 3 6.5Z" />
      <path d="M3 6.5V14L10 17.5V10" />
      <path d="M17 6.5V14L10 17.5" />
    </>
  ),
  "project-management": (
    <>
      <path d="M5 3v14" />
      <path d="M5 4h9l-2.5 3L14 10H5" />
    </>
  ),
  construction: (
    <>
      <path d="M4 17V6l4-2.5V17" />
      <path d="M8 8h4M8 11h4M8 14h4" />
      <path d="M12 17V9l4 1.5V17" />
    </>
  ),
  /* Stacked boxes — deliberately distinct from "procurement"'s single-diamond box glyph above, since both
     now sit side by side as real, separate departments (docs' "one consistent icon per department" rule
     only works if no two departments look alike). */
  inventory: (
    <>
      <rect x="7.5" y="3.5" width="5" height="5" rx="0.5" />
      <rect x="3" y="10.5" width="5" height="5" rx="0.5" />
      <rect x="12" y="10.5" width="5" height="5" rx="0.5" />
    </>
  ),
  "hr-payroll": (
    <>
      <circle cx="7" cy="7" r="2.4" />
      <path d="M2.6 16c0-2.4 2-4 4.4-4s4.4 1.6 4.4 4" />
      <circle cx="14.2" cy="7.2" r="2.1" />
      <path d="M10.8 16c.3-1.9 2-3.2 3.6-3.2 1.9 0 3.5 1.3 3.5 3.2" />
    </>
  ),
  equipment: (
    <>
      <path d="M13.3 4.3a3.5 3.5 0 0 0-4.5 4.5L3.5 14.1v2.4h2.4l5.3-5.3a3.5 3.5 0 0 0 4.5-4.5l-2.2 2.2-1.6-.4-.4-1.6 2.2-2.2Z" />
    </>
  ),
  crm: (
    <>
      <path d="M3 5.8a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2H8.5l-3.7 2.8v-2.8H5a2 2 0 0 1-2-2Z" />
      <path d="M6.5 8.3h7M6.5 10.8h4" />
    </>
  ),
};

export function DepartmentIcon({ icon, size = 18 }: DepartmentIconProps) {
  return (
    <svg
      className="pi-department-icon"
      width={size}
      height={size}
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {PATHS[icon]}
    </svg>
  );
}
