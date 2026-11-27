import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { CalendarDayResponse } from "../api/api";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";
import { formatDate } from "../utils/dates";

interface Props {
    onUnauthorized: () => void;
}

// Public holidays (by law, can't be changed) and company days off for one year.
// The system owner can add future company days off and remove them again.
export function CompanyDaysOff({ onUnauthorized }: Props) {
    const [year, setYear] = useState(new Date().getFullYear() + 1);
    const [days, setDays] = useState<CalendarDayResponse[] | null>(null);
    const [date, setDate] = useState("");
    const [name, setName] = useState("");
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const fail = useCallback(
        (err: unknown) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))),
        [onUnauthorized],
    );

    const load = useCallback(async () => {
        try {
            setDays((await api.api.getDaysOff({ year })).data);
        } catch (err) {
            fail(err);
        }
    }, [year, fail]);

    useEffect(() => {
        void load();
    }, [load]);

    async function add(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setMessage(null);
        setError(null);
        try {
            await api.api.addCompanyDayOff({ date, name });
            setMessage(`${name} on ${formatDate(date)} was added.`);
            setDate("");
            setName("");
            await load();
        } catch (err) {
            fail(err);
        }
    }

    async function remove(id: string) {
        setMessage(null);
        setError(null);
        try {
            await api.api.deleteCompanyDayOff(id);
            await load();
        } catch (err) {
            fail(err);
        }
    }

    return (
        <>
            <label>
                Year
                <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} />
            </label>

            {days && (
                <table>
                    <thead>
                    <tr>
                        <th>Date</th>
                        <th>Name</th>
                        <th></th>
                    </tr>
                    </thead>
                    <tbody>
                    {days.map((d) => (
                        <tr key={d.date}>
                            <td>{formatDate(d.date)}</td>
                            <td>{d.name}</td>
                            <td>
                                {/* only company days off can be removed, public holidays come from the law */}
                                {!d.isPublicHoliday && <button onClick={() => void remove(d.id!)}>Remove</button>}
                            </td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            )}

            <form className="form" onSubmit={add}>
                <label>
                    New company day off
                    <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
                </label>
                <label>
                    Name
                    <input value={name} onChange={(e) => setName(e.target.value)} required />
                </label>

                {error && <p className="error" role="alert">{error}</p>}
                {message && <p className="success">{message}</p>}

                <button type="submit">Add day off</button>
            </form>
        </>
    );
}