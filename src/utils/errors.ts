interface ProblemBody {
  title?: string | null;
  detail?: string | null;
  errors?: Record<string, string[]>;
}

/** Turns an error from the generated API client into a message we can show the user. */
export function errorMessage(error: unknown): string {
  const body = (error as { error?: ProblemBody } | null)?.error;
  if (body?.errors) return Object.values(body.errors).flat().join(" ");
  return body?.detail ?? body?.title ?? "Something went wrong. Try again.";
}

/** True when the API answered 401, e.g. because the token has expired. */
export function isUnauthorized(error: unknown): boolean {
  return (error as { status?: number } | null)?.status === 401;
}
