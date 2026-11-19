import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

interface ChatResponse {
  answer: string;
  intent?: string;
  tool_name?: string;
  tool_args?: { [key: string]: any };
}

@Component({
  selector: 'app-chat-container',
  templateUrl: './chat-container.component.html',
  styleUrls: ['./chat-container.component.scss']
})
export class ChatContainerComponent {
  messages: ChatMessage[] = [];
  message = '';
  isSending = false;

  constructor(private http: HttpClient) {}

  sendMessage(): void {
    const question = this.message.trim();
    if (!question || this.isSending) {
      return;
    }

    this.messages.push({ role: 'user', content: question });
    this.message = '';
    this.isSending = true;

    this.http.post<ChatResponse>(`${environment.aiUrl}/chat`, { message: question }).subscribe(
      (response) => {
        this.messages.push({ role: 'assistant', content: response.answer });
        this.isSending = false;
      },
      (error) => {
        const detail = error && error.error && error.error.detail
          ? error.error.detail
          : 'Không thể kết nối dịch vụ trợ lý.';
        this.messages.push({ role: 'assistant', content: detail });
        this.isSending = false;
      }
    );
  }
}
