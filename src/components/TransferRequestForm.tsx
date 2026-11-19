import { useState, type FormEvent } from "react";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";

interface Props {
  onSent: () => void;
  onUnauthorized: () => void;
}

// Lets the employee ask to move unused holiday hours to the next period.
// The rules (max 5 days, deadline, enough hours left) are checked by the API.
export function TransferRequestForm({ onSent, onUnauthorized }: Props) {
  const [account, setAccount] = useState("Weeks1To5");
  const [fromPeriodStart, setFromPeriodStart] = useState("");
  const [hours, setHours] = useState("");
  const [comment, setComment] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setError(null);
    setBusy(true);
    try {
      await api.api.createTransfer({ account, fromPeriodStart, hours: Number(hours), comment });
      setMessage(`Your request to transfer ${hours} hours was sent to the accountant.`);
      setFromPeriodStart("");
      setHours("");
      setComment("");
      onSent();
    } catch (err) {
      if (isUnauthorized(err)) onUnauthorized();
      else setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

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
        Holiday period starting
        <input type="date" value={fromPeriodStart} onChange={(e) => setFromPeriodStart(e.target.value)} required />
      </label>
      <label>
        Hours to transfer
        <input type="number" step="0.01" min="0" value={hours} onChange={(e) => setHours(e.target.value)} required />
      </label>
      <label>
        Comment (optional)
        <input value={comment} maxLength={1000} onChange={(e) => setComment(e.target.value)} />
      </label>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {message && <p className="success">{message}</p>}

      <button type="submit" disabled={busy}>
        {busy ? "Sending..." : "Send transfer request"}
      </button>
    </form>
  );
}