import { useCallback, useEffect, useState } from "react";
import type { MyRequestResponse, ScheduleResponse } from "../api/api";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { BalancePanel } from "../components/balance";
import { HolidayRequestForm } from "../components/HolidayRequestForm";
import { MyRequests } from "../components/MyRequests";
import { ScheduleTable } from "../components/schedule";
import { errorMessage, isUnauthorized } from "../utils/errors";

export function EmployeePage() {
  const { session, logout } = useAuth();
  const [requests, setRequests] = useState<MyRequestResponse[] | null>(null);
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

  useEffect(() => {
    void loadRequests();
    api.api
      .getMySchedules()
      .then(({ data }) => setSchedules(data))
      .catch(fail);
  }, [loadRequests, fail]);

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

      <section className="card wide">
        <h3>My work schedule</h3>
        <p className="muted">Only accounting and the system owner can change your schedule.</p>
        {schedules && <ScheduleTable schedules={schedules} />}
      </section>
    </>
  );
}
