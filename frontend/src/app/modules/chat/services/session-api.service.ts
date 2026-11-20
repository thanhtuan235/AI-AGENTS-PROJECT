import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ChatHistoryMessage, ChatSession } from '../chat.models';

@Injectable()
export class SessionApiService {
  private readonly baseUrl = `${environment.eofficeUrl}/api/chat/sessions`;

  constructor(private http: HttpClient) {}

  create(title?: string): Observable<ChatSession> {
    return this.http.post<ChatSession>(this.baseUrl, { title });
  }

  list(): Observable<ChatSession[]> {
    return this.http.get<ChatSession[]>(this.baseUrl);
  }

  getMessages(sessionId: number): Observable<ChatHistoryMessage[]> {
    return this.http.get<ChatHistoryMessage[]>(`${this.baseUrl}/${sessionId}/messages`);
  }

  saveMessages(sessionId: number, userMessage: string, assistantMessage: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/${sessionId}/messages`, {
      userMessage,
      assistantMessage,
    });
  }
}
