import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private readonly apiUrls = [
    environment.aiUrl,
    environment.backendUrl,
    environment.eofficeUrl,
  ];

  constructor(private readonly authService: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();
    const shouldAttachToken =
      !!token && this.apiUrls.some((url) => request.url.indexOf(url) === 0);

    if (!shouldAttachToken) {
      return next.handle(request);
    }

    return next.handle(
      request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    );
  }
}
