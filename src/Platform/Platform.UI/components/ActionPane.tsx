import type { ActionItem } from "../types";

/**
 * The Action Pane — a single command bar at the top of a record form
 * (docs/architecture/02-business-object-model.md #2.1). Per the architecture doc, its buttons apply to
 * the whole record and are "driven by the current FSM state + the user's security role/privilege — never
 * hard-coded per screen." This component is deliberately stateless: the consuming app decides which actions
 * are available right now (based on the document's lifecycle state and the signed-in user's permissions)
 * and passes only those. That is what makes a new BO transition surface its button everywhere automatically.
 */
interface ActionPaneProps {
  actions: ActionItem[];
  /** Accessible label for the toolbar — resolved by the consumer, never hardcoded here. */
  ariaLabel: string;
}

export function ActionPane({ actions, ariaLabel }: ActionPaneProps) {
  if (actions.length === 0) {
    return null;
  }

  return (
    <div className="pi-action-pane" role="toolbar" aria-label={ariaLabel}>
      {actions.map((action) => (
        <button
          key={action.key}
          type="button"
          className={
            "pi-action-pane__button" +
            (action.variant ? " pi-action-pane__button--" + action.variant : "")
          }
          onClick={action.onClick}
          disabled={action.isDisabled}
        >
          {action.label}
        </button>
      ))}
    </div>
  );
}
