import { useEffect, useState } from "react";
import "./index.css";
import { api } from "./api/client";

export function App() {
  const [status, setStatus] = useState("checking...");

  useEffect(() => {
    api
      .request<string>({ path: "/health", method: "GET", format: "text" })
      .then((res) => setStatus(res.data))
      .catch(() => setStatus("unreachable"));
  }, []);

  return (
    <div className="app">
      <h1>Business Esbjerg: Absence Planning</h1>
      <p>API status: {status}</p>
    </div>
  );
}

export default App;
