/**
 * Phase A1 — turns any HttpErrorResponse into a message a user can act on.
 *
 * Priority: the API's own message (GlobalExceptionMiddleware returns { message }),
 * then per-field validation errors ({ errors: { field: [msgs] } }), then a
 * status-specific explanation, then the caller's fallback.
 */
export function apiErrorMessage(err: any, fallback: string): string {
  const body = err?.error;

  if (typeof body?.message === 'string' && body.message.trim()) return body.message;

  // FluentValidation 400 shape: { errors: { Field: ["msg", ...], ... } }
  if (body?.errors && typeof body.errors === 'object') {
    const msgs = Object.values(body.errors)
      .flatMap((v: any) => (Array.isArray(v) ? v : [String(v)]))
      .filter(Boolean);
    if (msgs.length) return msgs.join(' ');
  }

  if (typeof body === 'string' && body.trim()) return body;

  switch (err?.status) {
    case 0: return 'Cannot reach the server — is the API running?';
    case 401: return 'Your session has expired — please sign in again.';
    case 403: return 'You do not have permission to perform this action.';
    case 404: return 'The requested API endpoint was not found (404) — the server may be running an older build.';
    case 409: return 'The record was changed by someone else — reload and try again.';
    case 500: return 'The server hit an unexpected error — please try again or check the server logs.';
  }

  return err?.status ? `${fallback} (HTTP ${err.status})` : fallback;
}
