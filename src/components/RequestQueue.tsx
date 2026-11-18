import { useCallback, useEffect, useState } from "react";
import type { QueueItemResponse } from "../api/api";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { accountLabel } from "./balance";

const kindLabels: Record<string, string> = {
  Holiday: "Holiday",
  Cancellation: "Cancel holiday",
  Transfer: "Transfer",
};

const hours = (value?: number) => (value ?? 0).toFixed(2);

interface Props {
  onUnauthorized: () => void;
}


// Requests waiting for the accountant. Declining needs a reason, so the employee knows why.
export function RequestQueue({ onUnauthorized }: Props) {
  const [queue, setQueue] = useState<QueueItemResponse[] | null>(null);
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fail = useCallback(
    (err: unknown) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))),
    [onUnauthorized],
  );

  const load = useCallback(async () => {
    try {
      setQueue((await api.api.getRequestQueue()).data);
    } catch (err) {
      fail(err);
    }
  }, [fail]);

  useEffect(() => {
    void load();
  }, [load]);

  
  async function decide(item: QueueItemResponse, approve: boolean) {
    setMessage(null);
    setError(null);
    const transfer = item.kind === "Transfer";
    const send = approve
      ? transfer ? api.api.approveTransferRequest : api.api.approveHolidayRequest
      : transfer ? api.api.declineTransferRequest : api.api.declineHolidayRequest;
    try {
      await send(item.id!, { reason: reasons[item.id!] || null });
      setMessage(`${item.employeeName}: ${approve ? "approved" : "declined"}.`);
      await load();
    } catch (err) {
      
      fail(err);
    }
  }

  if (!queue) return error ? <p className="error">{error}</p> : <p className="muted">Loading...</p>;

  return (
    <>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {message && <p className="success">{message}</p>}
      {queue.length === 0 && <p className="muted">Nothing is waiting for a decision.</p>}
      {queue.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Employee</th>
              <th>Request</th>
              <th>Dates</th>
              <th className="number">Hours</th>
              <th className="number">Balance</th>
              <th className="number">After</th>
              <th>Comment</th>
              <th>Reason</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {queue.map((item) => (
              <tr key={item.id}>
                <td>{item.employeeName}</td>
                <td>
                  {kindLabels[item.kind ?? ""] ?? item.kind} ({accountLabel(item.account)})
                </td>
                <td>
                  {item.kind === "Transfer" ? `Period from ${item.startDate}` : `${item.startDate} to ${item.endDate}`}
                </td>
                <td className="number">{hours(item.hours)}</td>
                <td className="number" title={`On ${item.balanceDate}`}>
                  {hours(item.balanceHours)}
                </td>
                <td className={(item.balanceAfterHours ?? 0) < 0 ? "number error" : "number"}>
                  {hours(item.balanceAfterHours)}
                </td>
                <td>{item.comment}</td>
                <td>
                  <input
                    value={reasons[item.id!] ?? ""}
                    maxLength={1000}
                    placeholder="Needed to decline"
                    onChange={(e) => setReasons({ ...reasons, [item.id!]: e.target.value })}
                  />
                </td>
                <td className="buttons">
                  <button type="button" onClick={() => void decide(item, true)}>
                    Approve
                  </button>
                  <button type="button" onClick={() => void decide(item, false)}>
                    Decline
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );
}
