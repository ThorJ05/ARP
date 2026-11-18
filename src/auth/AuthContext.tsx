import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import type { MeResponse } from "../api/api";
import { api } from "../api/client";

export type Role = "Employee" | "Accountant" | "SystemOwner";

interface Session {
  token: string;
  expiresAtUtc: string;
  me: MeResponse;
}

interface AuthContextValue {
  session: Session | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

// sessionStorage instead of localStorage: the login is gone when the browser tab is closed
const STORAGE_KEY = "arp.session";

function loadSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as Session;
    if (new Date(session.expiresAtUtc) <= new Date()) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => {
    const saved = loadSession();
    api.setSecurityData(saved?.token ?? null);
    return saved;
  });

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await api.api.login({ email, password });
    if (!data.token || !data.expiresAtUtc) throw new Error("The login answer had no token.");

    api.setSecurityData(data.token);
    try {
      const { data: me } = await api.api.getMe();
      const next: Session = { token: data.token, expiresAtUtc: data.expiresAtUtc, me };
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
      setSession(next);
    } catch (error) {
      api.setSecurityData(null);
      throw error;
    }
  }, []);

  const logout = useCallback(() => {
    sessionStorage.removeItem(STORAGE_KEY);
    api.setSecurityData(null);
    setSession(null);
  }, []);

  return <AuthContext.Provider value={{ session, login, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}

/** The page each role goes to after login. */
export function homePath(role: string | undefined): string {
  switch (role) {
    case "Employee":
      return "/employee";
    case "Accountant":
      return "/accountant";
    case "SystemOwner":
      return "/system-owner";
    default:
      return "/login";
  }
}

export function roleLabel(role: string | undefined): string {
  return role === "SystemOwner" ? "System owner" : (role ?? "");
}
