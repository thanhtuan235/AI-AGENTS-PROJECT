import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { AuthUser } from '../../../../core/models/auth.models';
import { ChatApiService } from '../../services/chat-api.service';
import { SessionApiService } from '../../services/session-api.service';
import { ChatHistoryMessage, ChatSession } from '../../chat.models';

@Component({
  selector: 'app-chat-container',
  templateUrl: './chat-container.component.html',
  styleUrls: ['./chat-container.component.scss']
})
export class ChatContainerComponent implements OnInit {
  messages: ChatHistoryMessage[] = [];
  sessions: ChatSession[] = [];
  message = '';
  isSending = false;
  isLoadingHistory = false;
  errorMessage = '';
  currentSessionId: number | null = null;

  private readonly sessionStorageKey = 'chat_session_id';

  constructor(
    private authService: AuthService,
    private router: Router,
    private chatApi: ChatApiService,
    private sessionApi: SessionApiService
  ) {}

  ngOnInit(): void {
    this.loadSessions();
  }

  get user(): AuthUser | null {
    return this.authService.getUser();
  }

  logout(): void {
    this.authService.logout();
    localStorage.removeItem(this.sessionStorageKey);
    this.router.navigate(['/login']);
  }

  loadSessions(): void {
    this.sessionApi.list().subscribe(
      (sessions) => {
        this.sessions = sessions;
        const storedId = Number(localStorage.getItem(this.sessionStorageKey));
        const session = sessions.find((item) => item.id === storedId) || sessions[0];

        if (session) {
          this.openSession(session);
        } else {
          this.createSession();
        }
      },
      () => {
        this.errorMessage = 'Không thể tải danh sách phiên trò chuyện.';
        this.createSession();
      }
    );
  }

  createSession(): void {
    this.sessionApi.create().subscribe(
      (session) => {
        this.sessions = [session, ...this.sessions];
        this.openSession(session);
      },
      () => {
        this.errorMessage = 'Không thể tạo phiên trò chuyện mới.';
      }
    );
  }

  openSession(session: ChatSession): void {
    this.currentSessionId = session.id;
    localStorage.setItem(this.sessionStorageKey, session.id.toString());
    this.isLoadingHistory = true;
    this.errorMessage = '';

    this.sessionApi.getMessages(session.id).subscribe(
      (messages) => {
        this.messages = messages;
        this.isLoadingHistory = false;
        this.scrollConversationToBottom();
      },
      () => {
        this.errorMessage = 'Không thể tải lịch sử phiên trò chuyện.';
        this.isLoadingHistory = false;
      }
    );
  }

  sendMessage(): void {
    const question = this.message.trim();
    if (!question || this.isSending) {
      return;
    }

    if (!this.currentSessionId) {
      this.errorMessage = 'Đang khởi tạo phiên trò chuyện. Vui lòng thử lại.';
      return;
    }

    this.messages.push({ role: 'user', content: question });
    this.message = '';
    this.isSending = true;
    this.errorMessage = '';
    this.scrollConversationToBottom();

    this.chatApi.send(question, this.currentSessionId).subscribe(
      (response) => {
        this.messages.push({
          role: 'assistant',
          content: response.answer,
          sources: response.sources || [],
        });
        this.isSending = false;
        this.sessionApi
          .saveMessages(this.currentSessionId as number, question, response.answer)
          .subscribe(
            () => {
              const session = this.sessions.find((item) => item.id === this.currentSessionId);
              if (session && (session.title === 'Cuộc trò chuyện mới' || !session.title)) {
                session.title = question.length > 50 ? question.slice(0, 50) + '...' : question;
              }
            },
            () => {
              this.errorMessage = 'Đã nhận câu trả lời nhưng không thể lưu lịch sử.';
            }
          );
        this.scrollConversationToBottom();
      },
      (error) => {
        const detail = error && error.error && error.error.detail
          ? error.error.detail
          : 'Không thể kết nối dịch vụ trợ lý.';
        this.errorMessage = detail;
        this.isSending = false;
        this.scrollConversationToBottom();
      }
    );
  }

  private scrollConversationToBottom(): void {
    setTimeout(() => {
      const conversation = document.querySelector('.chat-conversation');
      if (conversation) {
        conversation.scrollTop = conversation.scrollHeight;
      }
    });
  }
}
