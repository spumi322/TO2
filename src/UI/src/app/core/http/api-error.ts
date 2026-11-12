import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';

export type ApiErrorKind =
  | 'validation'
  | 'badRequest'
  | 'unauthorized'
  | 'forbidden'
  | 'notFound'
  | 'conflict'
  | 'rateLimited'
  | 'server'
  | 'offline';

// What the UI needs from a failed request, parsed from the backend's ProblemDetails
export interface ApiError {
  kind: ApiErrorKind;
  status: number;
  // Text to show: the server's detail for domain errors, a fixed text otherwise
  message: string;
  // 400 validation errors by field name as the server sends it (e.g. "NumberOfGroups", "Names[0]")
  fieldErrors: Record<string, string[]>;
  traceId?: string;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export const OFFLINE_MESSAGE = 'Cannot reach the server. Check your connection and try again.';
export const RATE_LIMIT_MESSAGE = 'Too many attempts. Try again in a minute.';
export const SERVER_ERROR_MESSAGE = 'Something went wrong on the server.';

export function toApiError(error: unknown): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return { kind: 'server', status: 0, message: SERVER_ERROR_MESSAGE, fieldErrors: {} };
  }

  const body = (typeof error.error === 'object' && error.error !== null ? error.error : {}) as ProblemDetails;
  const base = { status: error.status, fieldErrors: {}, traceId: body.traceId };
  const detail = body.detail ?? body.title;

  switch (true) {
    case error.status === 0:
      return { ...base, kind: 'offline', message: OFFLINE_MESSAGE };
    case error.status === 400 && !!body.errors:
      return {
        ...base,
        kind: 'validation',
        message: 'Please check the highlighted fields.',
        fieldErrors: body.errors ?? {},
      };
    case error.status === 400:
      return { ...base, kind: 'badRequest', message: detail ?? 'The request was not valid.' };
    case error.status === 401:
      return { ...base, kind: 'unauthorized', message: detail ?? 'Please sign in again.' };
    case error.status === 403:
      return { ...base, kind: 'forbidden', message: detail ?? 'You are not allowed to do this.' };
    case error.status === 404:
      return { ...base, kind: 'notFound', message: detail ?? 'Not found.' };
    case error.status === 409:
      return { ...base, kind: 'conflict', message: detail ?? 'The data changed meanwhile. Reload and try again.' };
    case error.status === 429:
      return { ...base, kind: 'rateLimited', message: RATE_LIMIT_MESSAGE };
    default:
      return { ...base, kind: 'server', message: SERVER_ERROR_MESSAGE };
  }
}

/**
 * Puts 400 field errors on the matching form controls (as the "server" error, names matched ignoring case).
 * Returns the messages no control took, to show at form level.
 */
export function applyFieldErrors(form: FormGroup, error: ApiError): string[] {
  const unmatched: string[] = [];

  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const control = findControl(form, field);
    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }

  return unmatched;
}

function findControl(form: FormGroup, field: string): AbstractControl | null {
  const name = Object.keys(form.controls).find((key) => key.toLowerCase() === field.toLowerCase());
  return name ? form.controls[name] : null;
}
