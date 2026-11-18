import { useEffect, useState } from "react";
import type { BalanceResponse } from "../api/api";
import { today } from "../utils/dates";
import { errorMessage, isUnauthorized } from "../utils/errors";

const accountLabels: Record<string, string> = {
  Weeks1To5: "Weeks 1-5",
  Week6: "Week 6",
};

export const accountLabel = (account?: string | null) => accountLabels[account ?? ""] ?? account ?? "";

const hours = (value?: number) => (value ?? 0).toFixed(2);

interface Props {
  /** Loads the balances on a date (today when the date is undefined). */
  load: (date?: string) => Promise<BalanceResponse[]>;
  /** Called when the login has expired (401). */
  onUnauthorized: () => void;
}

/** Balance per account in hours and days with expiry dates, today or on a chosen future date. */
export function BalancePanel({ load, onUnauthorized }: Props) {
  const [date, setDate] = useState("");
  const [balances, setBalances] = useState<BalanceResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setError(null);
    load(date || undefined)
      .then(setBalances)
      .catch((err) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))));
  }, [load, onUnauthorized, date]);

  return (
    <div className="balance">
      <label>
        Balance on
        <input type="date" value={date || today()} min={today()} onChange={(e) => setDate(e.target.value)} />
      </label>
      {date && date !== today() && (
        <p className="muted">
          The balance on {date}, with the holiday earned until then.{" "}
          <button type="button" onClick={() => setDate("")}>
            Back to today
          </button>
        </p>
      )}

      {error && <p className="error">{error}</p>}
      {!balances && !error && <p className="muted">Loading...</p>}
      {balances && <BalanceTable balances={balances} />}
      {balances && <ExpiryTable balances={balances} />}
    </div>
  );
}

function BalanceTable({ balances }: { balances: BalanceResponse[] }) {
  return (
    <table>
      <thead>
        <tr>
          <th>Account</th>
          <th className="number">Hours</th>
          <th className="number">Days</th>
          <th className="number">Can be used now</th>
          <th className="number">Expired</th>
        </tr>
      </thead>
      <tbody>
        {balances.map((b) => (
          <tr key={b.account}>
            <td>{accountLabel(b.account)}</td>
            <td className="number">{hours(b.hours)}</td>
            <td className="number">{hours(b.days)}</td>
            <td className="number">
              {hours(b.availableHours)} h / {hours(b.availableDays)} d
            </td>
            <td className="number">{b.expiredHours ? `${hours(b.expiredHours)} h` : "-"}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ExpiryTable({ balances }: { balances: BalanceResponse[] }) {
  const rows = balances.flatMap((b) => (b.buckets ?? []).map((bucket) => ({ account: b.account, ...bucket })));
  if (rows.length === 0) return null;

  return (
    <>
      <h4>Expiry dates</h4>
      <p className="muted">Week 6 can only be used from 1 May the year after it is earned.</p>
      <table>
        <thead>
          <tr>
            <th>Account</th>
            <th>Can be used from</th>
            <th>Expires</th>
            <th className="number">Hours</th>
            <th className="number">Days</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={`${r.account}-${r.expiresOn}`}>
              <td>{accountLabel(r.account)}</td>
              <td>{r.availableFrom}</td>
              <td>{r.expiresOn}</td>
              <td className="number">{hours(r.hours)}</td>
              <td className="number">{hours(r.days)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
