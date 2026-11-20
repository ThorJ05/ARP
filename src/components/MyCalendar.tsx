import { useEffect, useState } from "react";
import type { BalanceResponse, MyCalendarResponse } from "../api/api";
import { api } from "../api/client";
import { daysInMonth, isoDate, isWeekend, weekdayIndex } from "../utils/calendar";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { accountLabel } from "./balance";

const weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

const statusText: Record<string, string> = {
  Approved: "Holiday",
  Pending: "Waiting for approval",
  CancellationRequested: "Cancellation requested",
};
const statusLetter: Record<string, string> = { Approved: "H", Pending: "?", CancellationRequested: "C" };

interface Props {
  year: number;
  month: number;
  onUnauthorized: () => void;
}

const hours = (value?: number) => (value ?? 0).toFixed(2);

/** A month calendar of my own holiday, with my remaining holiday. */
export function MyCalendar({ year, month, onUnauthorized }: Props) {
  const [calendar, setCalendar] = useState<MyCalendarResponse | null>(null);
  const [balances, setBalances] = useState<BalanceResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fail = (err: unknown) => {
    if (isUnauthorized(err)) onUnauthorized();
    else setError(errorMessage(err));
  };

  useEffect(() => {
    let current = true;
    setError(null);
    setCalendar(null);
    api.api
      .getMyCalendar({ year, month })
      .then(({ data }) => current && setCalendar(data))
      .catch((err) => current && fail(err));
    return () => {
      current = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [year, month]);

  useEffect(() => {
    api.api
      .getMyBalances({})
      .then(({ data }) => setBalances(data))
      .catch(fail);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const off = new Set(calendar?.daysOff ?? []);
  const byDate = new Map((calendar?.days ?? []).map((d) => [d.date, d]));
  const cells: (number | null)[] = [
    ...Array.from({ length: weekdayIndex(year, month, 1) }, () => null),
    ...Array.from({ length: daysInMonth(year, month) }, (_, i) => i + 1),
  ];

  return (
    <div>
      {balances && (
        <p>
          <strong>Remaining holiday: </strong>
          {balances.map((b) => `${accountLabel(b.account)}: ${hours(b.hours)} h (${hours(b.days)} days)`).join(" · ")}
        </p>
      )}
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {!calendar && !error && <p className="muted">Loading...</p>}

      <div className="cal-grid" role="grid" aria-label="My holiday this month">
        {weekdays.map((d) => (
          <div key={d} className="cal-head" role="columnheader">
            {d}
          </div>
        ))}
        {cells.map((day, i) => {
          if (day === null) return <div key={`empty-${i}`} className="cal-cell empty" />;
          const date = isoDate(year, month, day);
          const away = byDate.get(date);
          const nonWorking = isWeekend(year, month, day) || off.has(date);
          return (
            <div key={date} className={nonWorking ? "cal-cell off" : "cal-cell"} role="gridcell">
              <span>{day}</span>
              {away && (
                <div>
                  <span
                    className={`cal-tag ${away.status}`}
                    title={`${statusText[away.status ?? ""] ?? away.status}, ${accountLabel(away.account)}`}
                  >
                    {statusLetter[away.status ?? ""] ?? "H"}
                  </span>
                  <div className="muted">{accountLabel(away.account)}</div>
                </div>
              )}
            </div>
          );
        })}
      </div>
      <p className="muted">
        H = approved holiday, ? = waiting for approval, C = cancellation requested. Grey days are weekends, public
        holidays or company days off.
      </p>
    </div>
  );
}
