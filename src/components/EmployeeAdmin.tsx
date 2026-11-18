import { useCallback, useEffect, useMemo, useState } from "react";
import type { EmployeeResponse, ScheduleResponse } from "../api/api";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { BalancePanel } from "./balance";
import { EmployeeTable } from "./EmployeeTable";
import { EndDateForm } from "./EndDateForm";
import { NewScheduleForm } from "./NewScheduleForm";
import { fromWorkWeek, fullTime, ScheduleTable, toWorkWeek } from "./schedule";

interface Props {
  /** Only the system owner can set an end date. */
  canSetEndDate: boolean;
  /** Change this number to load the list again (e.g. after creating an employee). */
  refreshKey?: number;
}

/** The employee list. The open employee shows balance, schedule history, a new schedule and the end date. */
export function EmployeeAdmin({ canSetEndDate, refreshKey = 0 }: Props) {
  const { logout } = useAuth();
  const [employees, setEmployees] = useState<EmployeeResponse[] | null>(null);
  const [selected, setSelected] = useState<EmployeeResponse | null>(null);
  const [history, setHistory] = useState<ScheduleResponse[] | null>(null);
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

  const open = useCallback(
    async (employee: EmployeeResponse) => {
      setSelected(employee);
      setMessage(null);
      setError(null);
      try {
        setHistory((await api.api.getScheduleHistory(employee.id!)).data);
      } catch (err) {
        fail(err);
      }
    },
    [fail],
  );

  const loadBalances = useCallback(
    async (date?: string) => (selected ? (await api.api.getEmployeeBalances(selected.id!, { date })).data : []),
    [selected],
  );

  // a new schedule starts from the one in use
  const currentTimes = useMemo(
    () => (history && history.length > 0 ? fromWorkWeek(history[history.length - 1]!.week) : fullTime),
    [history],
  );

  useEffect(() => {
    void loadEmployees();
  }, [loadEmployees, refreshKey]);

  // saves a change, then reloads the list and the open employee
  async function change(save: (id: string) => Promise<unknown>, done: string) {
    if (!selected) return;
    setMessage(null);
    setError(null);
    try {
      await save(selected.id!);
      const again = (await loadEmployees())?.find((e) => e.id === selected.id);
      if (again) await open(again);
      setMessage(done);
    } catch (err) {
      fail(err);
    }
  }

  return (
    <>
      <section className="card wide">
        <h2>Employees</h2>
        {!employees && !error && <p className="muted">Loading...</p>}
        {employees && <EmployeeTable employees={employees} selectedId={selected?.id} onOpen={(e) => void open(e)} />}
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

          <h3>Holiday balance</h3>
          <BalancePanel load={loadBalances} onUnauthorized={logout} />

          <h3>Work schedule history</h3>
          {history && <ScheduleTable schedules={history} />}

          <NewScheduleForm
            start={currentTimes}
            onSave={(validFrom, times) =>
              change(
                (id) => api.api.addSchedule(id, { validFrom, schedule: toWorkWeek(times) }),
                `New schedule from ${validFrom} saved.`,
              )
            }
          />

          {canSetEndDate && (
            <EndDateForm
              current={selected.terminationDate}
              onSave={(date) =>
                change(
                  (id) => api.api.setTerminationDate(id, { terminationDate: date }),
                  date ? `End date set to ${date}.` : "End date removed.",
                )
              }
            />
          )}
        </section>
      )}
    </>
  );
}
