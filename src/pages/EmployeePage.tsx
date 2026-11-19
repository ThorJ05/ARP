import { useCallback, useEffect, useState } from "react";
import type { MyRequestResponse, MyTransferResponse, ScheduleResponse } from "../api/api";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { BalancePanel } from "../components/balance";
import { HolidayRequestForm } from "../components/HolidayRequestForm";
import { MyRequests } from "../components/MyRequests";
import { ScheduleTable } from "../components/schedule";
import { TransferRequestForm } from "../components/TransferRequestForm";
import { errorMessage, isUnauthorized } from "../utils/errors";

export function EmployeePage() {
  const { session, logout } = useAuth();
  const [requests, setRequests] = useState<MyRequestResponse[] | null>(null);
  const [transfers, setTransfers] = useState<MyTransferResponse[] | null>(null);
  const [schedules, setSchedules] = useState<ScheduleResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fail = useCallback(
      (err: unknown) => (isUnauthorized(err) ? logout() : setError(errorMessage(err))),
      [logout],
  );

  const loadBalances = useCallback(async (date?: string) => (await api.api.getMyBalances({ date })).data, []);

  const loadRequests = useCallback(async () => {
    try {
      setRequests((await api.api.getMyRequests()).data);
    } catch (err) {
      fail(err);
    }
  }, [fail]);

  const loadTransfers = useCallback(async () => {
    try {
      setTransfers((await api.api.getMyTransfers()).data);
    } catch (err) {
      fail(err);
    }
  }, [fail]);

  useEffect(() => {
    void loadRequests();
    void loadTransfers();
    api.api
        .getMySchedules()
        .then(({ data }) => setSchedules(data))
        .catch(fail);
  }, [loadRequests, loadTransfers, fail]);

  async function cancel(request: MyRequestResponse) {
    setError(null);
    try {
      await api.api.cancelRequest(request.id!);
      await loadRequests();
    } catch (err) {
      fail(err);
    }
  }

  return (
      <>
        <section className="card">
          <h2>Hi {session?.me.employeeName}</h2>
          <h3>My holiday balance</h3>
          <BalancePanel load={loadBalances} onUnauthorized={logout} />
        </section>

        <section className="card">
          <h3>Request holiday</h3>
          <HolidayRequestForm onSent={() => void loadRequests()} onUnauthorized={logout} />
        </section>

        <section className="card wide">
          <h3>My holiday requests</h3>
          {error && (
              <p className="error" role="alert">
                {error}
              </p>
          )}
          {requests && <MyRequests requests={requests} onCancel={(r) => void cancel(r)} />}
        </section>

        <section className="card">
          <h3>Transfer holiday to next period</h3>
          <TransferRequestForm onSent={() => void loadTransfers()} onUnauthorized={logout} />
        </section>

        <section className="card wide">
          <h3>My transfer requests</h3>
          {transfers && transfers.length === 0 && <p className="muted">No transfer requests yet.</p>}
          {transfers && transfers.length > 0 && (
              <table>
                <thead>
                <tr>
                  <th>Balance</th>
                  <th>Period start</th>
                  <th>Hours</th>
                  <th>Status</th>
                </tr>
                </thead>
                <tbody>
                {transfers.map((t) => (
                    <tr key={t.id}>
                      <td>{t.account}</td>
                      <td>{t.fromPeriodStart}</td>
                      <td>{t.hours}</td>
                      <td>{t.status}</td>
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
      </>
  );
}