import { useState, type FormEvent } from "react";
import type { WorkWeek } from "../api/api";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { errorMessage, isUnauthorized } from "../utils/errors";

const days = ["monday", "tuesday", "wednesday", "thursday", "friday"] as const;
type Day = (typeof days)[number];
type DayTimes = Record<Day, { start: string; end: string }>;

// full time: Mon-Thu 08:00-15:30 (7.5 h), Fri 08:00-15:00 (7 h) = 37 h
const fullTime: DayTimes = {
  monday: { start: "08:00", end: "15:30" },
  tuesday: { start: "08:00", end: "15:30" },
  wednesday: { start: "08:00", end: "15:30" },
  thursday: { start: "08:00", end: "15:30" },
  friday: { start: "08:00", end: "15:00" },
};

const today = () => new Date().toISOString().slice(0, 10);

function minutes(time: string): number {
  const [h = 0, m = 0] = time.split(":").map(Number);
  return h * 60 + m;
}

function weeklyHours(times: DayTimes): number {
  return days.reduce((sum, d) => {
    const { start, end } = times[d];
    return start && end && end > start ? sum + (minutes(end) - minutes(start)) / 60 : sum;
  }, 0);
}

// the API wants "HH:mm:ss", and null for a day off
function toWorkWeek(times: DayTimes): WorkWeek {
  const week: WorkWeek = {};
  for (const d of days) {
    week[`${d}Start` as const] = times[d].start ? `${times[d].start}:00` : null;
    week[`${d}End` as const] = times[d].end ? `${times[d].end}:00` : null;
  }
  return week;
}

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

  function setTime(day: Day, field: "start" | "end", value: string) {
    setTimes((prev) => ({ ...prev, [day]: { ...prev[day], [field]: value } }));
  }

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

        <fieldset>
          <legend>Work schedule (leave a day empty if it's a day off)</legend>
          {days.map((d) => (
            <div className="day" key={d}>
              <span>{d[0]!.toUpperCase() + d.slice(1)}</span>
              <input type="time" value={times[d].start} onChange={(e) => setTime(d, "start", e.target.value)} />
              <span>-</span>
              <input type="time" value={times[d].end} onChange={(e) => setTime(d, "end", e.target.value)} />
            </div>
          ))}
          <p className="muted">{weeklyHours(times).toFixed(2)} hours a week</p>
        </fieldset>

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

      <section className="card">
        <h3>Global rules and company days off</h3>
        <p className="muted">Not built yet.</p>
      </section>
    </>
  );
}
