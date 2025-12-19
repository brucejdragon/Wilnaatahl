import React from "react";
import { Msg_$union, ViewModel, ViewState_ViewState } from "../generated/ViewModel/ViewModel";

type ViewModelContextData = {
  viewModel: ViewModel;
  state: ViewState_ViewState;
  dispatch: React.ActionDispatch<[msg: Msg_$union]>;
};

export const ViewModelContext = React.createContext<ViewModelContextData | null>(null);

export function useViewModel() {
  const context = React.useContext(ViewModelContext);

  if (!context) {
    throw new Error("useWorld() must be used within a WorldContext provider");
  }

  return context;
}
