import type { ScheduleResponse, WorkWeekDto } from "../api/api";
import { formatDate } from "../utils/dates";

export const days = ["monday", "tuesday", "wednesday", "thursday", "friday"] as const;
export type Day = (typeof days)[number];
export type DayTimes = Record<Day, { start: string; end: string }>;

// full time: Mon-Thu 08:00-15:30 (7.5 h), Fri 08:00-15:00 (7 h) = 37 h
export const fullTime: DayTimes = {
  monday: { start: "08:00", end: "15:30" },
  tuesday: { start: "08:00", end: "15:30" },
  wednesday: { start: "08:00", end: "15:30" },
  thursday: { start: "08:00", end: "15:30" },
  friday: { start: "08:00", end: "15:00" },
};

export const dayName = (d: Day) => d[0]!.toUpperCase() + d.slice(1);

/** "08:00:00" from the API -> "08:00" (empty for a day off). */
const shortTime = (time: string | null | undefined) => (time ? time.slice(0, 5) : "");

function minutes(time: string): number {
  const [h = 0, m = 0] = time.split(":").map(Number);
  return h * 60 + m;
}

export function weeklyHours(times: DayTimes): number {
  return days.reduce((sum, d) => {
    const { start, end } = times[d];
    return start && end && end > start ? sum + (minutes(end) - minutes(start)) / 60 : sum;
  }, 0);
}

/** The API wants "HH:mm:ss", and null for a day off. */
export function toWorkWeek(times: DayTimes): WorkWeekDto {
  const week: WorkWeekDto = {};
  for (const d of days) {
    week[`${d}Start` as const] = times[d].start ? `${times[d].start}:00` : null;
    week[`${d}End` as const] = times[d].end ? `${times[d].end}:00` : null;
  }
  return week;
}

export function fromWorkWeek(week: WorkWeekDto | undefined): DayTimes {
  const times = { ...fullTime };
  for (const d of days) {
    times[d] = { start: shortTime(week?.[`${d}Start`]), end: shortTime(week?.[`${d}End`]) };
  }
  return times;
}

/** Start and end time for each weekday. Leave a day empty if it's a day off. */
export function ScheduleFields({ times, onChange }: { times: DayTimes; onChange: (times: DayTimes) => void }) {
  const set = (day: Day, field: "start" | "end", value: string) =>
    onChange({ ...times, [day]: { ...times[day], [field]: value } });

  return (
    <fieldset>
      <legend>Work schedule (leave a day empty if it's a day off)</legend>
      {days.map((d) => (
        <div className="day" key={d}>
          <span>{dayName(d)}</span>
          <input
            type="time"
            aria-label={`${dayName(d)} start`}
            value={times[d].start}
            onChange={(e) => set(d, "start", e.target.value)}
          />
          <span>-</span>
          <input
            type="time"
            aria-label={`${dayName(d)} end`}
            value={times[d].end}
            onChange={(e) => set(d, "end", e.target.value)}
          />
        </div>
      ))}
      <p className="muted">{weeklyHours(times).toFixed(2)} hours a week</p>
    </fieldset>
  );
}

/** The schedule history, newest first. The schedule without an end date is the one in use. */
export function ScheduleTable({ schedules }: { schedules: ScheduleResponse[] }) {
  if (schedules.length === 0) return <p className="muted">No work schedule yet.</p>;

  return (
    <table>
      <thead>
        <tr>
          <th>From</th>
          <th>To</th>
          {days.map((d) => (
            <th key={d}>{dayName(d).slice(0, 3)}</th>
          ))}
          <th className="number">Hours/week</th>
        </tr>
      </thead>
      <tbody>
        {[...schedules].reverse().map((s) => (
          <tr key={s.id} className={s.validTo ? undefined : "current"}>
            <td>{formatDate(s.validFrom)}</td>
            <td>{s.validTo ? formatDate(s.validTo) : "now"}</td>
            {days.map((d) => {
              const start = shortTime(s.week?.[`${d}Start`]);
              const end = shortTime(s.week?.[`${d}End`]);
              return <td key={d}>{start ? `${start}-${end}` : "off"}</td>;
            })}
            <td className="number">{(s.weeklyHours ?? 0).toFixed(2)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
