import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';
import { applyFieldErrors, OFFLINE_MESSAGE, RATE_LIMIT_MESSAGE, SERVER_ERROR_MESSAGE, toApiError } from './api-error';

function httpError(status: number, error: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error });
}

describe('toApiError', () => {
  it('maps 400 validation errors to field errors', () => {
    const error = toApiError(
      httpError(400, { title: 'One or more validation errors occurred.', errors: { NumberOfGroups: ['Too many groups.'] } }),
    );

    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({ NumberOfGroups: ['Too many groups.'] });
  });

  it('uses the server detail for domain errors', () => {
    const error = toApiError(httpError(409, { title: 'Conflict Occurred', detail: 'Only 2 slots left.' }));

    expect(error).toMatchObject({ kind: 'conflict', status: 409, message: 'Only 2 slots left.' });
  });

  it('uses a fixed text for 429', () => {
    expect(toApiError(httpError(429, { detail: 'whatever' }))).toMatchObject({
      kind: 'rateLimited',
      message: RATE_LIMIT_MESSAGE,
    });
  });

  it('keeps the traceId of a 500 but not its detail', () => {
    const error = toApiError(httpError(500, { detail: 'An unexpected error occurred.', traceId: '0HN:1' }));

    expect(error).toMatchObject({ kind: 'server', message: SERVER_ERROR_MESSAGE, traceId: '0HN:1' });
  });

  it('treats status 0 as offline', () => {
    expect(toApiError(httpError(0))).toMatchObject({ kind: 'offline', message: OFFLINE_MESSAGE });
  });

  it('handles a non-JSON body', () => {
    expect(toApiError(httpError(404, 'Not Found'))).toMatchObject({ kind: 'notFound', message: 'Not found.' });
  });
});

describe('applyFieldErrors', () => {
  it('sets errors on controls matched ignoring case and returns the rest', () => {
    const form = new FormGroup({ numberOfGroups: new FormControl(2), name: new FormControl('Cup') });
    const error = toApiError(
      httpError(400, { errors: { NumberOfGroups: ['Too many groups.'], Other: ['Something else.'] } }),
    );

    const unmatched = applyFieldErrors(form, error);

    expect(form.controls.numberOfGroups.errors).toEqual({ server: 'Too many groups.' });
    expect(form.controls.name.errors).toBeNull();
    expect(unmatched).toEqual(['Something else.']);
  });
});
