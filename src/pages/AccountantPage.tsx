import { useAuth } from "../auth/AuthContext";
import { EmployeeAdmin } from "../components/EmployeeAdmin";
import { RequestQueue } from "../components/RequestQueue";
import { HolidayReport } from "../components/HolidayReport";
import { IllnessForm } from "../components/IllnessForm";

export function AccountantPage() {
  const { logout } = useAuth();

  return (
    <>
      <section className="card wide">
        <h2>Requests waiting for a decision</h2>
        <p className="muted">
          Balance is the balance on the last day of the holiday, with earned and approved holiday included.
        </p>
        <RequestQueue onUnauthorized={logout} />
      </section>
        <section className="card wide">
            <h2>Holiday to settle</h2>
            <p className="muted">Expired hours must be paid out. Employees with a last day must have their remaining hours settled.</p>
            <HolidayReport onUnauthorized={logout} />
        </section>
      <section className="card wide">
        <h2>Illness during holiday</h2>
        <p className="muted">Sick days during approved holiday are given back, except the first 5 (waiting days).</p>
        <IllnessForm onUnauthorized={logout} />
      </section>
      <EmployeeAdmin canSetEndDate={false} />
    </>
  );
}
