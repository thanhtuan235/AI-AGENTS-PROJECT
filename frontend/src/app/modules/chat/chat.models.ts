export interface ChatSession {
  id: number;
  title: string;
  startTime: string;
}

export interface ChatHistoryMessage {
  id?: number;
  role: 'user' | 'assistant';
  content: string;
  timestamp?: string;
  sources?: ChatSource[];
}

export interface ChatSource {
  title: string;
  url?: string;
  excerpt?: string;
}

export interface ChatApiResponse {
  answer: string;
  intent?: string;
  tool_name?: string;
  tool_args?: { [key: string]: any };
  sources?: ChatSource[];
}
