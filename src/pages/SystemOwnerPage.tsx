import { useState, type FormEvent } from "react";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { EmployeeAdmin } from "../components/EmployeeAdmin";
import { fullTime, ScheduleFields, toWorkWeek, type DayTimes } from "../components/schedule";
import { errorMessage, isUnauthorized } from "../utils/errors";

const today = () => new Date().toISOString().slice(0, 10);

export function SystemOwnerPage() {
  const { logout } = useAuth();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [hireDate, setHireDate] = useState(today());
  const [times, setTimes] = useState<DayTimes>(fullTime);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setError(null);
    setBusy(true);
    try {
      await api.api.createEmployee({ name, email, password, hireDate, schedule: toWorkWeek(times) });
      setMessage(`${name} was created and can now log in with ${email}.`);
      setName("");
      setEmail("");
      setPassword("");
      setRefreshKey((k) => k + 1); // show the new employee in the list
    } catch (err) {
      if (isUnauthorized(err)) logout();
      else setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <form className="card form" onSubmit={onSubmit}>
        <h2>Create employee</h2>

        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>

        <label>
          Email
          <input
            type="email"
            placeholder="name@businessesbjerg.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </label>

        <label>
          Password (at least 12 characters)
          <input
            type="password"
            autoComplete="new-password"
            minLength={12}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>

        <label>
          Hire date
          <input type="date" value={hireDate} onChange={(e) => setHireDate(e.target.value)} required />
        </label>

        <ScheduleFields times={times} onChange={setTimes} />

        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        {message && <p className="success">{message}</p>}

        <button type="submit" disabled={busy}>
          {busy ? "Creating..." : "Create employee"}
        </button>
      </form>

      <EmployeeAdmin canSetEndDate refreshKey={refreshKey} />

      <section className="card wide">
        <h3>Global rules and company days off</h3>
        <p className="muted">Not built yet.</p>
      </section>
    </>
  );
}
