import { trait, World } from "koota";

// Used for entities that represent buttons on the toolbar.
export const Button = trait({ sortOrder: 0, label: "", disabled: false });
export const UndoButton = trait();
export const RedoButton = trait();
export const SelectModeButton = trait({ multiSelect: true });

export function defineControls(world: World) {
  world.spawn(Button({ sortOrder: 0, label: "Undo", disabled: true }), UndoButton);
  world.spawn(Button({ sortOrder: 1, label: "Redo", disabled: true }), RedoButton);
  world.spawn(
    Button({ sortOrder: 2, label: "Multi-select", disabled: false }),
    SelectModeButton({ multiSelect: false })
  );
}
