import { useEffect, useState } from "react";
import { Outlet } from "react-router";
import { api } from "../api/client";
import { roleLabel, useAuth } from "../auth/AuthContext";

export function Layout() {
  const { session, logout } = useAuth();
  const [apiStatus, setApiStatus] = useState("checking...");

  useEffect(() => {
    api
      .request<string>({ path: "/health", method: "GET", format: "text" })
      .then((res) => setApiStatus(res.data))
      .catch(() => setApiStatus("unreachable"));
  }, []);

  return (
    <div className="app">
      <header className="topbar">
        <h1>Business Esbjerg: Absence Planning</h1>
        {session && (
          <div className="user">
            <span>
              {session.me.employeeName ?? session.me.email} · {roleLabel(session.me.role)}
            </span>
            <button type="button" onClick={logout}>
              Log out
            </button>
          </div>
        )}
      </header>

      <main>
        <Outlet />
      </main>

      <footer className="muted">API status: {apiStatus}</footer>
    </div>
  );
}
