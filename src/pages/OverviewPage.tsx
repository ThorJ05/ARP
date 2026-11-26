import { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { MonthNav } from "../components/MonthNav";
import { MyCalendar } from "../components/MyCalendar";
import { TeamGrid } from "../components/TeamGrid";
import { currentMonth } from "../utils/calendar";

type Tab = "mine" | "team";

export function OverviewPage() {
  const { logout } = useAuth();
  const [tab, setTab] = useState<Tab>("mine");
  const [{ year, month }, setMonth] = useState(currentMonth);

  return (
    <section className="card wide">
      <div className="overview-header">
        <h2>Holiday overview</h2>

        <div className="tabs" role="group" aria-label="Choose view">
          <button
            type="button"
            aria-pressed={tab === "mine"}
            className={tab === "mine" ? "active" : undefined}
            onClick={() => setTab("mine")}
          >
            My calendar
          </button>
          <button
            type="button"
            aria-pressed={tab === "team"}
            className={tab === "team" ? "active" : undefined}
            onClick={() => setTab("team")}
          >
            Team
          </button>
        </div>
      </div>

      <MonthNav year={year} month={month} onChange={(y, m) => setMonth({ year: y, month: m })} />

      {tab === "mine" ? (
        <MyCalendar year={year} month={month} onUnauthorized={logout} />
      ) : (
        <TeamGrid year={year} month={month} onUnauthorized={logout} />
      )}
    </section>
  );
}
