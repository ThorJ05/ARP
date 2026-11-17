import { useEffect, useState } from "react";
import type { BalanceResponse, MyRequestResponse, ScheduleView } from "../api/api";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { ScheduleTable } from "../components/schedule";
import { errorMessage, isUnauthorized } from "../utils/errors";

const accountLabels: Record<string, string> = {
  Weeks1To5: "Weeks 1-5",
  Week6: "Week 6",
};

export function EmployeePage() {
  const { session, logout } = useAuth();
  const [balances, setBalances] = useState<BalanceResponse[] | null>(null);
  const [requests, setRequests] = useState<MyRequestResponse[] | null>(null);
  const [schedules, setSchedules] = useState<ScheduleView[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([api.api.getMyBalances(), api.api.getMyRequests(), api.api.getMySchedules()])
      .then(([balanceResponse, requestResponse, scheduleResponse]) => {
        setBalances(balanceResponse.data);
        setRequests(requestResponse.data);
        setSchedules(scheduleResponse.data);
      })
      .catch((err) => (isUnauthorized(err) ? logout() : setError(errorMessage(err))));
  }, [logout]);

  return (
    <>
      <section className="card">
        <h2>Hi {session?.me.employeeName}</h2>
        <h3>My holiday balance</h3>
        {error && <p className="error">{error}</p>}
        {!balances && !error && <p className="muted">Loading...</p>}
        {balances && (
          <table>
            <thead>
              <tr>
                <th>Account</th>
                <th className="number">Hours</th>
              </tr>
            </thead>
            <tbody>
              {balances.map((b) => (
                <tr key={b.account}>
                  <td>{accountLabels[b.account ?? ""] ?? b.account}</td>
                  <td className="number">{(b.hours ?? 0).toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="card wide">
        <h3>My work schedule</h3>
        <p className="muted">Only accounting and the system owner can change your schedule.</p>
        {schedules && <ScheduleTable schedules={schedules} />}
      </section>

      <section className="card">
        <h3>My holiday requests</h3>
        {requests && requests.length === 0 && <p className="muted">No holiday requests yet.</p>}
        {requests && requests.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>From</th>
                <th>To</th>
                <th>Account</th>
                <th className="number">Hours</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {requests.map((r) => (
                <tr key={r.id}>
                  <td>{r.startDate}</td>
                  <td>{r.endDate}</td>
                  <td>{accountLabels[r.account ?? ""] ?? r.account}</td>
                  <td className="number">{(r.hours ?? 0).toFixed(2)}</td>
                  <td>{r.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}
