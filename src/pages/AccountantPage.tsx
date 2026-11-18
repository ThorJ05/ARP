import { useAuth } from "../auth/AuthContext";
import { EmployeeAdmin } from "../components/EmployeeAdmin";
import { RequestQueue } from "../components/RequestQueue";

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

      <EmployeeAdmin canSetEndDate={false} />
    </>
  );
}
