import { useEffect, useState } from "react";
import type { HolidayReportRow } from "../api/api";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";

interface Props {
    onUnauthorized: () => void;
}

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

    // shows "-" when the employee isn't leaving
    const show = (hours?: number | null) => (hours == null ? "-" : hours.toFixed(2));

    if (error) return <p className="error" role="alert">{error}</p>;
    if (!rows) return null;

    return (
        <table>
            <thead>
            <tr>
                <th>Employee</th>
                <th>Last day</th>
                <th>Expired weeks 1-5</th>
                <th>Expired week 6</th>
                <th>Left weeks 1-5</th>
                <th>Left week 6</th>
            </tr>
            </thead>
            <tbody>
            {rows.map((r) => (
                <tr key={r.name}>
                    <td>{r.name}</td>
                    <td>{r.terminationDate ?? "-"}</td>
                    <td>{show(r.expiredWeeks1To5)}</td>
                    <td>{show(r.expiredWeek6)}</td>
                    <td>{show(r.leftWeeks1To5)}</td>
                    <td>{show(r.leftWeek6)}</td>
                </tr>
            ))}
            </tbody>
        </table>
    );
}