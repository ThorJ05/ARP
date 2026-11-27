import type { MyTransferResponse } from "../api/api";
import { accountLabel } from "./balance";
import { formatDate } from "../utils/dates";

const statusLabels: Record<string, string> = {
  Pending: "Waiting for approval",
  Approved: "Approved",
  Rejected: "Rejected",
};

export function MyTransfers({ transfers }: { transfers: MyTransferResponse[] }) {
  if (transfers.length === 0) return <p className="muted">No transfer requests yet.</p>;

  return (
    <table>
      <thead>
        <tr>
          <th>Balance</th>
          <th>Period from</th>
          <th className="number">Hours</th>
          <th>Status</th>
          <th>Decision</th>
        </tr>
      </thead>
      <tbody>
        {transfers.map((t) => (
          <tr key={t.id}>
            <td>{accountLabel(t.account)}</td>
            <td>{formatDate(t.fromPeriodStart)}</td>
            <td className="number">{(t.hours ?? 0).toFixed(2)}</td>
            <td>{statusLabels[t.status ?? ""] ?? t.status}</td>
            <td>{t.decisionReason}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
