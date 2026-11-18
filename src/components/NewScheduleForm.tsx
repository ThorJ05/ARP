import { useEffect, useState, type FormEvent } from "react";
import { today } from "../utils/dates";
import { ScheduleFields, type DayTimes } from "./schedule";

interface Props {
  /** The times the form starts with (the schedule in use, so only the changes need typing). */
  start: DayTimes;
  onSave: (validFrom: string, times: DayTimes) => Promise<void>;
}

export function NewScheduleForm({ start, onSave }: Props) {
  const [validFrom, setValidFrom] = useState(today());
  const [times, setTimes] = useState<DayTimes>(start);

  useEffect(() => setTimes(start), [start]);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void onSave(validFrom, times);
  }

  return (
    <form className="form" onSubmit={submit}>
      <h3>New schedule</h3>
      <p className="muted">
        Old schedules are never changed. The remaining balance is converted so the number of days stays the same.
      </p>
      <label>
        Used from
        <input type="date" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} required />
      </label>
      <ScheduleFields times={times} onChange={setTimes} />
      <button type="submit">Save new schedule</button>
    </form>
  );
}
