import { useEffect, useState } from "react";
import type { TeamOverviewResponse } from "../api/api";
import { api } from "../api/client";
import { daysInMonth, isoDate, isWeekend, monthNames, weekdayIndex } from "../utils/calendar";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { formatDate } from "../utils/dates";

const letters = ["M", "T", "W", "T", "F", "S", "S"];

interface Props {
  year: number;
  month: number;
  onUnauthorized: () => void;
}

/** Employees as rows and days as columns, with the number of people away under each day. No balances. */
export function TeamGrid({ year, month, onUnauthorized }: Props) {
  const [data, setData] = useState<TeamOverviewResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let current = true;
    setError(null);
    setData(null);
    api.api
      .getTeamOverview({ year, month })
      .then(({ data: result }) => current && setData(result))
      .catch((err) => {
        if (!current) return;
        if (isUnauthorized(err)) onUnauthorized();
        else setError(errorMessage(err));
      });
    return () => {
      current = false;
    };
  }, [year, month, onUnauthorized]);

  if (error)
    return (
      <p className="error" role="alert">
        {error}
      </p>
    );
  if (!data) return <p className="muted">Loading...</p>;

  const days = Array.from({ length: daysInMonth(year, month) }, (_, i) => i + 1);
  const off = new Set(data.daysOff ?? []);
  const counts = new Map((data.absentPerDay ?? []).map((c) => [c.date, c.absent ?? 0]));
  const rows = data.rows ?? [];

  if (rows.length === 0) return <p className="muted">No employees to show for this month.</p>;

  return (
    <div className="team-wrap">
      <table className="team-grid" aria-label={`Team holiday ${monthNames[month - 1]} ${year}`}>
        <thead>
          <tr>
            <th className="name">Employee</th>
            {days.map((d) => (
              <th key={d} className={isWeekend(year, month, d) || off.has(isoDate(year, month, d)) ? "off" : undefined}>
                {d}
                <br />
                <span className="muted">{letters[weekdayIndex(year, month, d)]}</span>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => {
            const away = new Set(r.absentDays ?? []);
            return (
              <tr key={r.employeeId} className={r.isMe ? "me" : undefined}>
                <td className="name">
                  {r.name}
                  {r.isMe ? " (me)" : ""}
                </td>
                {days.map((d) => {
                  const date = isoDate(year, month, d);
                  const isAway = away.has(date);
                  const nonWorking = isWeekend(year, month, d) || off.has(date);
                  return (
                    <td
                      key={d}
                      className={isAway ? "away" : nonWorking ? "off" : undefined}
                      title={isAway ? `${r.name}: holiday ${formatDate(date)}` : undefined}
                    >
                      {isAway ? "H" : ""}
                    </td>
                  );
                })}
              </tr>
            );
          })}
        </tbody>
        <tfoot>
          <tr>
            <td className="name">Away</td>
            {days.map((d) => {
              const count = counts.get(isoDate(year, month, d)) ?? 0;
              return (
                <td key={d} className={count === 0 ? "muted" : undefined}>
                  {count}
                </td>
              );
            })}
          </tr>
        </tfoot>
      </table>
      <p className="muted">H = approved holiday. Grey days are weekends, public holidays or company days off.</p>
    </div>
  );
}
