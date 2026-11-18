import type { ReactNode } from "react";
import { Navigate } from "react-router";
import { homePath, useAuth, type Role } from "./AuthContext";

/**
 * Only shows the page to a logged-in user with the right role, otherwise it sends them to the login page
 * or to their own page. This is only for the user experience: the API checks the role again on every request.
 */
export function RequireRole({ role, children }: { role: Role; children: ReactNode }) {
  const { session } = useAuth();

  if (!session) return <Navigate to="/login" replace />;
  if (session.me.role !== role) return <Navigate to={homePath(session.me.role)} replace />;

  return <>{children}</>;
}
