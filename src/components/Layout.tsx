import { useEffect, useState } from "react";
import { NavLink, Outlet } from "react-router";
import { api } from "../api/client";
import { roleLabel, useAuth } from "../auth/AuthContext";

// The pages each role can open, shown as the menu in the header.
const menu: Record<string, { to: string; label: string }[]> = {
  Employee: [
    { to: "/employee", label: "My holiday" },
    { to: "/overview", label: "Holiday overview" },
  ],
  Accountant: [{ to: "/accountant", label: "Requests and reports" }],
  SystemOwner: [{ to: "/system-owner", label: "Employees and rules" }],
};

export function Layout() {
  const { session, logout } = useAuth();
  const [apiStatus, setApiStatus] = useState("checking...");
  const menuItems = menu[session?.me.role ?? ""] ?? [];

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

        {menuItems.length > 0 && (
          <nav className="menu" aria-label="Main menu">
            {menuItems.map((item) => (
              // NavLink marks the page you are on with class="active" and aria-current="page"
              <NavLink key={item.to} to={item.to}>
                {item.label}
              </NavLink>
            ))}
          </nav>
        )}
      </header>

      <main>
        <Outlet />
      </main>

      <footer className="muted">API status: {apiStatus}</footer>
    </div>
  );
}
