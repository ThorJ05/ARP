import { EmployeeAdmin } from "../components/EmployeeAdmin";

export function AccountantPage() {
  return (
    <>
      <section className="card wide">
        <h2>Accountant</h2>
        <p>Here you will approve or reject holiday requests, cancellations and transfers.</p>
        <p className="muted">Handling requests is not built yet.</p>
      </section>

      <EmployeeAdmin canSetEndDate={false} />
    </>
  );
}
