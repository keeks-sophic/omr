export type SelectedElement =
  | { type: "node"; id: string }
  | { type: "path"; id: string }
  | { type: "point"; id: string }
  | { type: "qr"; id: string };

export type EditorTool = "select" | "pan" | "node" | "path" | "point" | "qr";

