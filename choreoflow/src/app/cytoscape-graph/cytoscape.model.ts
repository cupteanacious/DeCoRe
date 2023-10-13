export interface NodeData {
    id: string;
    initiator: string;
    action: string;
    receivers: string;
    dom?: HTMLDivElement;
    markings?: {
      pending: boolean;
      executed: boolean;
      included: boolean;
    }
  }