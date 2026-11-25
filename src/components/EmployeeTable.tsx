import type { EmployeeResponse } from "../api/api";
import { formatDate } from "../utils/dates";

interface Props {
  employees: EmployeeResponse[];
  selectedId?: string;
  onOpen: (employee: EmployeeResponse) => void;
}

export function EmployeeTable({ employees, selectedId, onOpen }: Props) {
  return (
    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>Email</th>
          <th>Hired</th>
          <th>Left</th>
          <th className="number">Hours/week</th>
          <th className="number">Weeks 1-5</th>
          <th className="number">Week 6</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {employees.map((e) => (
          <tr key={e.id} className={e.id === selectedId ? "current" : undefined}>
            <td>{e.name}</td>
            <td>{e.email}</td>
            <td>{formatDate(e.hireDate)}</td>
            <td>{formatDate(e.terminationDate)}</td>
            <td className="number">{e.currentWeeklyHours?.toFixed(2) ?? "-"}</td>
            <td className="number">{(e.weeks1To5Hours ?? 0).toFixed(2)}</td>
            <td className="number">{(e.week6Hours ?? 0).toFixed(2)}</td>
            <td>
              <button type="button" onClick={() => onOpen(e)}>
                Open
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
