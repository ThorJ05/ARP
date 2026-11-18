import { useEffect, useState } from "react";

interface Props {
  current?: string | null;
  /** null removes the end date. */
  onSave: (date: string | null) => Promise<void>;
}

export function EndDateForm({ current, onSave }: Props) {
  const [endDate, setEndDate] = useState(current ?? "");

  useEffect(() => setEndDate(current ?? ""), [current]);

  return (
    <div className="form">
      <h3>End date</h3>
      <label>
        Last day of employment
        <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
      </label>
      <div className="buttons">
        <button type="button" disabled={!endDate} onClick={() => void onSave(endDate)}>
          Save end date
        </button>
        {current && (
          <button type="button" onClick={() => void onSave(null)}>
            Remove end date
          </button>
        )}
      </div>
    </div>
  );
}
