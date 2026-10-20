import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiError, toApiError } from './api-error';

// Shows a failed request as a toast; server errors carry the traceId to quote in a report
@Injectable({ providedIn: 'root' })
export class ErrorToast {
  private readonly messages = inject(MessageService);

  show(error: unknown, summary = 'Action failed'): ApiError {
    const apiError = toApiError(error);
    const detail =
      apiError.kind === 'server' && apiError.traceId
        ? `${apiError.message} Trace id: ${apiError.traceId}`
        : apiError.message;

    this.messages.add({ severity: 'error', summary, detail, life: apiError.kind === 'server' ? 10_000 : 5_000 });
    return apiError;
  }
}
