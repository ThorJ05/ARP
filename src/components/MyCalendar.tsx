import { useEffect, useState } from "react";
import type { BalanceResponse, MyCalendarResponse } from "../api/api";
import { api } from "../api/client";
import { daysInMonth, isoDate, isWeekend, weekdayIndex } from "../utils/calendar";
import { today } from "../utils/dates";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { accountLabel } from "./balance";

const weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

// the short text shown in a calendar day
const statusText: Record<string, string> = {
  Approved: "Holiday",
  Pending: "Waiting",
  CancellationRequested: "Cancelling",
};

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

  const todayDate = today();
  const off = new Set(calendar?.daysOff ?? []);
  const byDate = new Map((calendar?.days ?? []).map((d) => [d.date, d]));
  const cells: (number | null)[] = [
    ...Array.from({ length: weekdayIndex(year, month, 1) }, () => null),
    ...Array.from({ length: daysInMonth(year, month) }, (_, i) => i + 1),
  ];

  return (
    <div>
      {balances && (
        <div className="stats">
          {balances.map((b) => (
            <div key={b.account} className="stat">
              <span className="stat-label">{accountLabel(b.account)} left</span>
              <span className="stat-value">{hours(b.days)} days</span>
              <span className="muted">{hours(b.hours)} hours</span>
            </div>
          ))}
        </div>
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
          const isToday = date === todayDate;

          const classes = ["cal-cell"];
          if (nonWorking) classes.push("off");
          if (isToday) classes.push("today");
          if (away) classes.push("away", away.status ?? "");

          return (
            <div
              key={date}
              className={classes.join(" ")}
              role="gridcell"
              aria-current={isToday ? "date" : undefined}
            >
              <span className="cal-day">{day}</span>
              {away && (
                <span className="cal-label">
                  {statusText[away.status ?? ""] ?? away.status}
                  <small>{accountLabel(away.account)}</small>
                </span>
              )}
            </div>
          );
        })}
      </div>
      <ul className="legend">
        <li>
          <span className="swatch Approved" /> Holiday
        </li>
        <li>
          <span className="swatch Pending" /> Waiting for approval
        </li>
        <li>
          <span className="swatch CancellationRequested" /> Cancellation requested
        </li>
        <li>
          <span className="swatch off" /> Weekend, public holiday or company day off
        </li>
      </ul>
    </div>
  );
}
