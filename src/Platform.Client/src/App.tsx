import { useState, useEffect, useRef } from 'react';
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

  // Inactivity timeout states
  const [showInactivityModal, setShowInactivityModal] = useState(false);
  const [countdown, setCountdown] = useState(60);
  const countdownIntervalRef = useRef<any>(null);
  const inactivityTimeoutRef = useRef<any>(null);

  const handleLogout = () => {
    fetch('/Account/Logout', { method: 'POST' })
      .then(() => {
        setUser(null);
        window.location.href = '/';
      });
  };

  // Inactivity threshold: 14 minutes of absolute inactivity.
  // After 14 minutes, show warning modal for 60 seconds (total 15 minutes of inactivity).
  const INACTIVITY_LIMIT = 14 * 60 * 1000; 
  const WARNING_LIMIT = 60; 

  const resetInactivityTimer = () => {
    if (showInactivityModal) return; 
    
    if (inactivityTimeoutRef.current) {
      clearTimeout(inactivityTimeoutRef.current);
    }
    
    inactivityTimeoutRef.current = setTimeout(() => {
      setShowInactivityModal(true);
      setCountdown(WARNING_LIMIT);
    }, INACTIVITY_LIMIT);
  };

  useEffect(() => {
    if (!user) {
      if (inactivityTimeoutRef.current) clearTimeout(inactivityTimeoutRef.current);
      if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current);
      setShowInactivityModal(false);
      return;
    }

    const events = ['mousemove', 'keydown', 'mousedown', 'click', 'scroll', 'touchstart'];
    events.forEach(e => window.addEventListener(e, resetInactivityTimer));

    resetInactivityTimer();

    return () => {
      events.forEach(e => window.removeEventListener(e, resetInactivityTimer));
      if (inactivityTimeoutRef.current) clearTimeout(inactivityTimeoutRef.current);
      if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current);
    };
  }, [user, showInactivityModal]);

  // Handle countdown progression
  useEffect(() => {
    if (showInactivityModal) {
      countdownIntervalRef.current = setInterval(() => {
        setCountdown(prev => {
          if (prev <= 1) {
            clearInterval(countdownIntervalRef.current);
            handleLogout();
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } else {
      if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current);
    }

    return () => {
      if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current);
    };
  }, [showInactivityModal]);

  const keepSessionAlive = () => {
    setShowInactivityModal(false);
    resetInactivityTimer();
  };

  useEffect(() => {
    // Carica l'utente corrente all'avvio
    fetch('/Account/User')
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
          <Route path="*" element={<Login onLoginSuccess={(u) => {
            setUser(u);
            window.location.href = '/';
          }} />} />
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
          <Route path="/kiosk/compile/:id" element={<KioskCompile user={user} />} />
          <Route path="/kiosk/history/:id" element={<KioskHistory />} />
          <Route path="/admin/users" element={<AdminUsers />} />
          <Route path="/admin/permissions" element={<AdminPermissions />} />
          <Route path="/developer/tools" element={<DeveloperTools />} />
          {/* Fallback per rotte inesistenti */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>

      {/* Modale di avviso inattività */}
      {showInactivityModal && (
        <>
          <div className="modal-backdrop fade show" style={{ zIndex: 1060 }}></div>
          <div className="modal fade show d-block" style={{ zIndex: 1070 }} tabIndex={-1}>
            <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: '400px' }}>
              <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px', background: 'rgba(255,255,255,0.95)', backdropFilter: 'blur(10px)' }}>
                <div className="modal-body text-center p-4">
                  <div className="mb-3 text-warning">
                    <i className="bi bi-shield-fill-exclamation" style={{ fontSize: '3rem' }}></i>
                  </div>
                  <h5 className="fw-bold text-dark mb-2">Sessione in scadenza</h5>
                  <p className="small text-muted mb-4">
                    Sei inattivo da molto tempo. Per ragioni di sicurezza, verrai disconnesso tra:
                  </p>
                  <div className="fs-1 fw-bold text-danger mb-4 countdown-display animate-pulse">
                    {countdown} <span className="fs-5 text-muted fw-normal">secondi</span>
                  </div>
                  <div className="progress mb-4" style={{ height: '6px' }}>
                    <div 
                      className="progress-bar bg-danger progress-bar-striped progress-bar-animated" 
                      role="progressbar" 
                      style={{ width: `${(countdown / WARNING_LIMIT) * 100}%`, transition: 'width 1s linear' }}
                    ></div>
                  </div>
                  <button 
                    type="button" 
                    className="btn btn-primary w-100 py-2.5 fw-bold" 
                    onClick={keepSessionAlive}
                    style={{ borderRadius: '12px' }}
                  >
                    Rimani Connesso
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </BrowserRouter>
  );
}
