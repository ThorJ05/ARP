import {
  HOLIDAY_YEAR_START_MONTH,
  currentMonth,
  holidayYearOf,
  monthNames,
  shiftMonth,
} from "../utils/calendar";

interface Props {
  year: number;
  month: number;
  onChange: (year: number, month: number) => void;
}

/** Switch between months and holiday years. */
export function MonthNav({ year, month, onChange }: Props) {
  const holidayYear = holidayYearOf(year, month);
  const now = currentMonth();
  const thisHolidayYear = holidayYearOf(now.year, now.month);
  const years = [...new Set([...Array.from({ length: 6 }, (_, i) => thisHolidayYear - 2 + i), holidayYear])].sort(
    (a, b) => a - b,
  );

  // the 12 months of the chosen holiday year, May first
  const months = Array.from({ length: 12 }, (_, i) => {
    const m = ((HOLIDAY_YEAR_START_MONTH - 1 + i) % 12) + 1;
    return { year: m >= HOLIDAY_YEAR_START_MONTH ? holidayYear : holidayYear + 1, month: m };
  });

  function go(delta: number) {
    const next = shiftMonth(year, month, delta);
    onChange(next.year, next.month);
  }

  return (
    <nav className="month-nav" aria-label="Choose month">
      <div className="month-nav-top">
        <div className="month-switcher">
          <button type="button" className="arrow" aria-label="Previous month" onClick={() => go(-1)}>
            ‹
          </button>
          <strong className="month-title">
            {monthNames[month - 1]} {year}
          </strong>
          <button type="button" className="arrow" aria-label="Next month" onClick={() => go(1)}>
            ›
          </button>
          <button type="button" className="today-button" onClick={() => onChange(now.year, now.month)}>
            Today
          </button>
        </div>

        <label className="holiday-year">
          Holiday year
          <select value={holidayYear} onChange={(e) => onChange(Number(e.target.value), HOLIDAY_YEAR_START_MONTH)}>
            {years.map((y) => (
              <option key={y} value={y}>
                {y}-{y + 1}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="month-strip">
        {months.map((m) => {
          const active = m.year === year && m.month === month;
          return (
            <button
              key={`${m.year}-${m.month}`}
              type="button"
              className={active ? "active" : undefined}
              aria-pressed={active}
              onClick={() => onChange(m.year, m.month)}
            >
              {monthNames[m.month - 1]!.slice(0, 3)}
            </button>
          );
        })}
      </div>
    </nav>
  );
}
