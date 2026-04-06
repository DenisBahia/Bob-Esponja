import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { SharingContextService } from '../services/sharing-context.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  const sharingCtx = inject(SharingContextService);

  let headers = req.headers;
  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  const viewAsId = sharingCtx.viewAsUserId();
  if (viewAsId !== null) {
    headers = headers.set('X-View-As-User', String(viewAsId));
  }

  return next(req.clone({ headers }));
};
