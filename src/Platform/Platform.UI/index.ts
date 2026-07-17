// Platform.UI barrel export — the single import surface for consumers.
// Apps.Shell imports from "@platform/ui" (resolved via a Vite alias + tsconfig path mapping; see
// docs/architecture/02-business-object-model.md). Components are pure presentation: they receive already-translated strings and
// data structures as props and never call a translation function themselves, keeping the dependency
// one-directional (app -> design system, never reverse).

export { ShellBar } from "./components/ShellBar";
export { NavigationPane } from "./components/NavigationPane";
export { ActionPane } from "./components/ActionPane";
export { FastTabs } from "./components/FastTabs";
export { SplitView } from "./components/SplitView";

export type {
  LanguageCode,
  LanguageOption,
  AriaLabels,
  NavItem,
  NavArea,
  NavModule,
  ActionItem,
  ActionVariant,
  FastTabItem,
} from "./types";
