import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import "./index.css";
import { AuthProvider, homePath, useAuth } from "./auth/AuthContext";
import { RequireRole } from "./auth/RequireRole";
import { Layout } from "./components/Layout";
import { AccountantPage } from "./pages/AccountantPage";
import { EmployeePage } from "./pages/EmployeePage";
import { LoginPage } from "./pages/LoginPage";
import { SystemOwnerPage } from "./pages/SystemOwnerPage";

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/login" element={<LoginPage />} />
            <Route
              path="/employee"
              element={
                <RequireRole role="Employee">
                  <EmployeePage />
                </RequireRole>
              }
            />
            <Route
              path="/accountant"
              element={
                <RequireRole role="Accountant">
                  <AccountantPage />
                </RequireRole>
              }
            />
            <Route
              path="/system-owner"
              element={
                <RequireRole role="SystemOwner">
                  <SystemOwnerPage />
                </RequireRole>
              }
            />
            <Route path="*" element={<HomeRedirect />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

// "/" and unknown addresses: go to the login page or to the page for the user's role
function HomeRedirect() {
  const { session } = useAuth();
  return <Navigate to={homePath(session?.me.role)} replace />;
}

export default App;
