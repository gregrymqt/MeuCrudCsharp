import type { FieldValues } from "react-hook-form";

// Define as opções para os seletores
export type TerminalModule = 'course' | 'video' | 'mercadopago';
export type TerminalAction = 'list' | 'create' | 'dashboard';
export type TerminalFilter = 'all' | 'active' | 'archived';

// O formato dos dados do formulário
export interface TerminalFormData extends FieldValues {
  module: TerminalModule;
  action: TerminalAction;
  filter: TerminalFilter;
}