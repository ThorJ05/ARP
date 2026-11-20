import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { ApprovedHolidayResponse } from "../api/api";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";

interface Props {
  onUnauthorized: () => void;
}

// The accountant picks an approved holiday and the days the employee was sick. The API works out
// which days are given back (after the waiting days).
export function IllnessForm({ onUnauthorized }: Props) {
  const [holidays, setHolidays] = useState<ApprovedHolidayResponse[] | null>(null);
  const [requestId, setRequestId] = useState("");
  const [firstSickDay, setFirstSickDay] = useState("");
  const [lastSickDay, setLastSickDay] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const fail = useCallback(
    (err: unknown) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))),
    [onUnauthorized],
  );

  useEffect(() => {
    api.api
      .getApprovedHoliday()
      .then(({ data }) => setHolidays(data))
      .catch(fail);
  }, [fail]);

  const selected = holidays?.find((h) => h.id === requestId);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setError(null);
    setBusy(true);
    try {
      const { data } = await api.api.registerIllness(requestId, { firstSickDay, lastSickDay });
      setMessage(`${selected?.employeeName} got ${(data.hoursGivenBack ?? 0).toFixed(2)} hours back.`);
      setFirstSickDay("");
      setLastSickDay("");
    } catch (err) {
      fail(err);
    } finally {
      setBusy(false);
    }
  }

  if (!holidays) return null;
  if (holidays.length === 0) return <p className="muted">No approved holiday yet.</p>;

  return (
    <form className="form" onSubmit={submit}>
      <label>
        Holiday
        <select value={requestId} onChange={(e) => setRequestId(e.target.value)} required>
          <option value="" disabled>
            Choose a holiday
          </option>
          {holidays.map((h) => (
            <option key={h.id} value={h.id}>
              {h.employeeName}: {h.startDate} to {h.endDate}
            </option>
          ))}
        </select>
      </label>
      <label>
        First sick day
        <input
          type="date"
          value={firstSickDay}
          min={selected?.startDate}
          max={selected?.endDate}
          onChange={(e) => setFirstSickDay(e.target.value)}
          required
        />
      </label>
      <label>
        Last sick day
        <input
          type="date"
          value={lastSickDay}
          min={firstSickDay || selected?.startDate}
          max={selected?.endDate}
          onChange={(e) => setLastSickDay(e.target.value)}
          required
        />
      </label>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {message && <p className="success">{message}</p>}

      <button type="submit" disabled={!requestId || busy}>
        {busy ? "Saving..." : "Register illness"}
      </button>
    </form>
  );
}
