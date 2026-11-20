import { useEffect, useState } from "react";
import type { HolidayReportRow } from "../api/api";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";

interface Props {
  onUnauthorized: () => void;
}

// "-" when there is nothing to show, e.g. remaining hours for someone who isn't leaving
const show = (hours?: number | null) => (hours == null ? "-" : hours.toFixed(2));

// Expired hours for everyone, and remaining hours for employees with an end date.
export function HolidayReport({ onUnauthorized }: Props) {
  const [rows, setRows] = useState<HolidayReportRow[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.api
      .getHolidayReport()
      .then(({ data }) => setRows(data))
      .catch((err) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))));
  }, [onUnauthorized]);

  if (error)
    return (
      <p className="error" role="alert">
        {error}
      </p>
    );
  if (!rows) return <p className="muted">Loading...</p>;

  return (
    <table>
      <thead>
        <tr>
          <th>Employee</th>
          <th>Last day</th>
          <th className="number">Expired weeks 1-5</th>
          <th className="number">Expired week 6</th>
          <th className="number">Left weeks 1-5</th>
          <th className="number">Left week 6</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={`${r.name}-${r.terminationDate}`}>
            <td>{r.name}</td>
            <td>{r.terminationDate ?? "-"}</td>
            <td className="number">{show(r.expiredWeeks1To5)}</td>
            <td className="number">{show(r.expiredWeek6)}</td>
            <td className="number">{show(r.leftWeeks1To5)}</td>
            <td className="number">{show(r.leftWeek6)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
