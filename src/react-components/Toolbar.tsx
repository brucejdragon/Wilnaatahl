import React from "react";
import { Msg_ToggleSelection, Msg_Undo, Msg_Redo } from "../generated/ViewModel/ViewModel";
import { useViewModel } from "../context/viewModelContext";

export default function Toolbar() {
  const { viewModel, state, dispatch } = useViewModel();

  return (
    <div style={{ margin: "8px" }}>
      <button onClick={() => dispatch(Msg_Undo())} disabled={!viewModel.CanUndo(state)}>
        Undo
      </button>
      <button
        onClick={() => dispatch(Msg_Redo())}
        disabled={!viewModel.CanRedo(state)}
        style={{ marginLeft: "8px" }}
      >
        Redo
      </button>
      <button
        style={{ marginLeft: "8px" }}
        onClick={() => {
          const nextMode = viewModel.IsSingleSelectEnabled(state) ? "multiSelect" : "singleSelect";
          dispatch(Msg_ToggleSelection(nextMode));
        }}
      >
        {viewModel.IsSingleSelectEnabled(state) ? "Multi-select" : "Single-select"}
      </button>
    </div>
  );
}
