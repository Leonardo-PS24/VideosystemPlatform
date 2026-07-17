import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import KioskDashboard from './pages/KioskDashboard';
import KioskCompile from './pages/KioskCompile';
import KioskHistory from './pages/KioskHistory';
import AdminUsers from './pages/AdminUsers';
import AdminPermissions from './pages/AdminPermissions';
import DeveloperTools from './pages/DeveloperTools';

import SetPassword from './pages/SetPassword';

interface UserData {
  username: string;
  email: string;
  fullName: string;
  roles: string[];
}

export default function App() {
  const [user, setUser] = useState<UserData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Carica l'utente corrente all'avvio
    fetch('/Account/CurrentUser')
      .then((res) => {
        if (res.ok) return res.json();
        throw new Error();
      })
      .then((data) => {
        setUser(data);
        setLoading(false);
      })
      .catch(() => {
        setUser(null);
        setLoading(false);
      });
  }, []);

  const handleLogout = () => {
    fetch('/Account/Logout', { method: 'POST' })
      .then(() => setUser(null));
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center bg-light" style={{ minHeight: '100vh' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Caricamento...</span>
        </div>
      </div>
    );
  }

  // Se non autenticato, mostra il login o la rotta pubblica di impostazione password
  if (!user) {
    return (
      <BrowserRouter>
        <Routes>
          <Route path="/set-password" element={<SetPassword />} />
          <Route path="*" element={<Login onLoginSuccess={(u) => setUser(u)} />} />
        </Routes>
      </BrowserRouter>
    );
  }

  // Se autenticato, carica il layout principale e le rotte
  return (
    <BrowserRouter>
      <Layout user={user} onLogout={handleLogout}>
        <Routes>
          <Route path="/" element={<Dashboard user={user} />} />
          <Route path="/kiosk" element={<KioskDashboard user={user} />} />
          <Route path="/kiosk/compile/:id" element={<KioskCompile />} />
          <Route path="/kiosk/history/:id" element={<KioskHistory />} />
          <Route path="/admin/users" element={<AdminUsers />} />
          <Route path="/admin/permissions" element={<AdminPermissions />} />
          <Route path="/developer/tools" element={<DeveloperTools />} />
          {/* Fallback per rotte inesistenti */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}
