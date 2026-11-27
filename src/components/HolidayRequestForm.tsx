import { useEffect, useState, type FormEvent } from "react";
import type { RequestCheckResponse } from "../api/api";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { formatDate } from "../utils/dates";

interface Props {
  onSent: () => void;
  onUnauthorized: () => void;
}

// Shows the cost and any broken rules while the employee fills in the dates.
export function HolidayRequestForm({ onSent, onUnauthorized }: Props) {
  const [account, setAccount] = useState("Weeks1To5");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [comment, setComment] = useState("");
  const [check, setCheck] = useState<RequestCheckResponse | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setCheck(null);
    setError(null);
    if (!startDate || !endDate) return;

    api.api
      .previewRequest({ account, startDate, endDate })
      .then(({ data }) => setCheck(data))
      .catch((err) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))));
  }, [account, startDate, endDate, onUnauthorized]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setError(null);
    setBusy(true);
    try {
      await api.api.createRequest({ account, startDate, endDate, comment });
      setMessage(`Your request for ${formatDate(startDate)} to ${formatDate(endDate)} was sent to the accountant.`);
      setStartDate("");
      setEndDate("");
      setComment("");
      onSent();
    } catch (err) {
      if (isUnauthorized(err)) onUnauthorized();
      else setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const canSend = check !== null && (check.errors?.length ?? 0) === 0 && !busy;

  return (
    <form className="form" onSubmit={submit}>
      <label>
        Balance
        <select value={account} onChange={(e) => setAccount(e.target.value)}>
          <option value="Weeks1To5">Weeks 1-5</option>
          <option value="Week6">Week 6</option>
        </select>
      </label>
      <label>
        First day
        <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
      </label>
      <label>
        Last day
        <input type="date" value={endDate} min={startDate} onChange={(e) => setEndDate(e.target.value)} required />
      </label>
      <label>
        Comment (optional)
        <input value={comment} maxLength={1000} onChange={(e) => setComment(e.target.value)} />
      </label>

      {check && (check.workingDays ?? 0) > 0 && (
        <p className="muted">
          {check.workingDays} working days, costs {check.hours?.toFixed(2)} hours. Weekends, public holidays and
          company days off are not counted.
        </p>
      )}
      {check?.errors?.map((e) => (
        <p key={e} className="error">
          {e}
        </p>
      ))}
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {message && <p className="success">{message}</p>}

      <button type="submit" disabled={!canSend}>
        {busy ? "Sending..." : "Send request"}
      </button>
    </form>
  );
}
