import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { EmployeeView, ScheduleView } from "../api/api";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { fromWorkWeek, fullTime, ScheduleFields, ScheduleTable, toWorkWeek, type DayTimes } from "./schedule";

interface Props {
  /** Only the system owner can set an end date. */
  canSetEndDate: boolean;
  /** Change this number to load the list again (e.g. after creating an employee). */
  refreshKey?: number;
}

const today = () => new Date().toISOString().slice(0, 10);

/** List of employees, their schedule history, a new schedule and (for the system owner) the end date. */
export function EmployeeAdmin({ canSetEndDate, refreshKey = 0 }: Props) {
  const { logout } = useAuth();
  const [employees, setEmployees] = useState<EmployeeView[] | null>(null);
  const [selected, setSelected] = useState<EmployeeView | null>(null);
  const [history, setHistory] = useState<ScheduleView[] | null>(null);
  const [validFrom, setValidFrom] = useState(today());
  const [times, setTimes] = useState<DayTimes>(fullTime);
  const [endDate, setEndDate] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fail = useCallback(
    (err: unknown) => (isUnauthorized(err) ? logout() : setError(errorMessage(err))),
    [logout],
  );

  const loadEmployees = useCallback(async () => {
    try {
      const { data } = await api.api.getEmployees();
      setEmployees(data);
      return data;
    } catch (err) {
      fail(err);
      return null;
    }
  }, [fail]);

  const select = useCallback(
    async (employee: EmployeeView) => {
      setSelected(employee);
      setMessage(null);
      setError(null);
      setEndDate(employee.terminationDate ?? "");
      try {
        const { data } = await api.api.getScheduleHistory(employee.id!);
        setHistory(data);
        // start the new schedule from the one in use, so only the changes need typing
        setTimes(data.length > 0 ? fromWorkWeek(data[data.length - 1]!.week) : fullTime);
      } catch (err) {
        fail(err);
      }
    },
    [fail],
  );

  useEffect(() => {
    void loadEmployees();
  }, [loadEmployees, refreshKey]);

  // reload the list and the selected employee after a change
  async function reload(message: string) {
    const list = await loadEmployees();
    const again = list?.find((e) => e.id === selected?.id);
    if (again) await select(again);
    setMessage(message);
  }

  async function addSchedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    setMessage(null);
    setError(null);
    try {
      await api.api.addSchedule(selected.id!, { validFrom, schedule: toWorkWeek(times) });
      await reload(`New schedule from ${validFrom} saved.`);
    } catch (err) {
      fail(err);
    }
  }

  async function saveEndDate(date: string | null) {
    if (!selected) return;
    setMessage(null);
    setError(null);
    try {
      await api.api.setTerminationDate(selected.id!, { terminationDate: date });
      await reload(date ? `End date set to ${date}.` : "End date removed.");
    } catch (err) {
      fail(err);
    }
  }

  return (
    <>
      <section className="card wide">
        <h2>Employees</h2>
        {!employees && !error && <p className="muted">Loading...</p>}
        {employees && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Hired</th>
                <th>Left</th>
                <th className="number">Hours/week</th>
                <th className="number">Weeks 1-5</th>
                <th className="number">Week 6</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {employees.map((e) => (
                <tr key={e.id} className={e.id === selected?.id ? "current" : undefined}>
                  <td>{e.name}</td>
                  <td>{e.email}</td>
                  <td>{e.hireDate}</td>
                  <td>{e.terminationDate ?? ""}</td>
                  <td className="number">{e.currentWeeklyHours?.toFixed(2) ?? "-"}</td>
                  <td className="number">{(e.weeks1To5Hours ?? 0).toFixed(2)}</td>
                  <td className="number">{(e.week6Hours ?? 0).toFixed(2)}</td>
                  <td>
                    <button type="button" onClick={() => void select(e)}>
                      Schedule
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {selected && (
        <section className="card wide">
          <h2>{selected.name}</h2>
          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}
          {message && <p className="success">{message}</p>}

          <h3>Work schedule history</h3>
          {history && <ScheduleTable schedules={history} />}

          <form className="form" onSubmit={addSchedule}>
            <h3>New schedule</h3>
            <p className="muted">
              Old schedules are never changed. The remaining balance is converted so the number of days stays the same.
            </p>
            <label>
              Used from
              <input type="date" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} required />
            </label>
            <ScheduleFields times={times} onChange={setTimes} />
            <button type="submit">Save new schedule</button>
          </form>

          {canSetEndDate && (
            <div className="form">
              <h3>End date</h3>
              <label>
                Last day of employment
                <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
              </label>
              <div className="buttons">
                <button type="button" disabled={!endDate} onClick={() => void saveEndDate(endDate)}>
                  Save end date
                </button>
                {selected.terminationDate && (
                  <button type="button" onClick={() => void saveEndDate(null)}>
                    Remove end date
                  </button>
                )}
              </div>
            </div>
          )}
        </section>
      )}
    </>
  );
}
