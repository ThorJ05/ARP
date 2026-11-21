import { useState } from "react";
import { CreateEmployeeForm } from "../components/CreateEmployeeForm";
import { EmployeeAdmin } from "../components/EmployeeAdmin";
import { useAuth } from "../auth/AuthContext";
import { RulesAdmin } from "../components/RulesAdmin";

export function SystemOwnerPage() {
    const {logout}= useAuth();
  // bumped after an employee is created, so the list loads again
    const [refreshKey, setRefreshKey] = useState(0);

  return (
    <>
        
      <CreateEmployeeForm onCreated={() => setRefreshKey((k) => k + 1)} />
        

      <EmployeeAdmin canSetEndDate refreshKey={refreshKey} />

      <section className="card wide">
        <h3>Global rules and company days off</h3>
        <RulesAdmin onUnauthorized={logout} />
      </section>
    </>
  );
}
