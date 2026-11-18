import type { MyRequestResponse } from "../api/api";
import { accountLabel } from "./balance";

const statusLabels: Record<string, string> = {
  Pending: "Waiting for approval",
  Approved: "Approved",
  Rejected: "Rejected",
  CancellationRequested: "Cancellation requested",
  Cancelled: "Cancelled",
};

interface Props {
  requests: MyRequestResponse[];
  onCancel: (request: MyRequestResponse) => void;
}

export function MyRequests({ requests, onCancel }: Props) {
  if (requests.length === 0) return <p className="muted">No holiday requests yet.</p>;

  return (
    <table>
      <thead>
        <tr>
          <th>From</th>
          <th>To</th>
          <th>Balance</th>
          <th className="number">Hours</th>
          <th>Status</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {requests.map((r) => (
          <tr key={r.id}>
            <td>{r.startDate}</td>
            <td>{r.endDate}</td>
            <td>{accountLabel(r.account)}</td>
            <td className="number">{(r.hours ?? 0).toFixed(2)}</td>
            <td>
              {statusLabels[r.status ?? ""] ?? r.status}
              {r.decisionReason && <span className="muted"> ({r.decisionReason})</span>}
            </td>
            <td>
              {r.canCancel && (
                <button type="button" onClick={() => onCancel(r)}>
                  {r.status === "Approved" ? "Ask to cancel" : "Withdraw"}
                </button>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
