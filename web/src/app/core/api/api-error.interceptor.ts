import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { ApiError } from '../models';

/** Shows API failures consistently while preserving the observable error contract. */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const messages = inject(MessageService);
  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        const apiError = error.error as Partial<ApiError> | null;
        messages.add({
          severity: 'error',
          summary: 'تعذر تنفيذ الطلب',
          detail: apiError?.description || statusMessage(error.status),
          life: 7000,
        });
      }
      return throwError(() => error);
    }),
  );
};

function statusMessage(status: number): string {
  if (status === 0) return 'تعذر الاتصال بالخادم. تحقق من تشغيل الواجهة الخلفية.';
  if (status === 403) return 'ليس لديك صلاحية لتنفيذ هذا الإجراء.';
  if (status === 404) return 'العنصر المطلوب غير موجود.';
  if (status === 409) return 'لا يمكن تنفيذ الإجراء لوجود بيانات مرتبطة.';
  if (status >= 500) return 'حدث خطأ داخلي في الخادم. حاول مرة أخرى.';
  return 'تحقق من البيانات المدخلة ثم حاول مرة أخرى.';
}
