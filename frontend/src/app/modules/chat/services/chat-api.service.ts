import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ChatApiResponse } from '../chat.models';

@Injectable()
export class ChatApiService {
  constructor(private http: HttpClient) {}

  send(message: string, sessionId: number): Observable<ChatApiResponse> {
    return this.http.post<ChatApiResponse>(`${environment.aiUrl}/chat`, {
      message,
      session_id: sessionId.toString(),
    });
  }
}
