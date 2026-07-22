import React, { useState, useEffect, useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

interface LayoutProps {
  user: {
    username: string;
    email: string;
    fullName: string;
    roles: string[];
  } | null;
  onLogout: () => void;
  children: React.ReactNode;
}

export default function Layout({ user, onLogout, children }: LayoutProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Collasso della sidebar (persistito nel localStorage)
  const [collapsed, setCollapsed] = useState(() => {
    return localStorage.getItem('sidebar_collapsed') === 'true';
  });

  // Stato apertura dropdown utente
  const [dropdownOpen, setDropdownOpen] = useState(false);

  // Chiudi il dropdown cliccando all'esterno
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setDropdownOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const toggleSidebar = () => {
    const nextState = !collapsed;
    setCollapsed(nextState);
    localStorage.setItem('sidebar_collapsed', String(nextState));
  };

  useEffect(() => {
    if (window.innerWidth < 992) {
      setCollapsed(true);
    }
  }, [location.pathname]);

  const handleLogoutClick = (e: React.MouseEvent) => {
    e.preventDefault();
    onLogout();
  };

  const handleBack = () => {
    const path = location.pathname;
    if (path.startsWith('/kiosk/')) {
      navigate('/kiosk');
    } else {
      navigate('/');
    }
  };

  const isActive = (path: string) => {
    return location.pathname === path ? 'active' : '';
  };

  const getPageTitle = () => {
    const path = location.pathname;
    if (path === '/') return 'Dashboard';
    if (path === '/kiosk') return 'Kiosk Checklist';
    if (path.startsWith('/kiosk/compile/')) return 'Compila Checklist';
    if (path.startsWith('/kiosk/history/')) return 'Storico Checklist';
    if (path === '/admin/users') return 'Gestione Utenti';
    if (path === '/admin/permissions') return 'Gestione Permessi';
    if (path === '/developer/tools') return 'Strumenti Dev';
    return 'Portale Videosystem';
  };

  return (
    <div className="app-container">
      {/* Overlay per tablet/mobile quando la sidebar è aperta */}
      {!collapsed && (
        <div className="sidebar-overlay d-lg-none" onClick={() => setCollapsed(true)}></div>
      )}

      {/* Sidebar */}
      <aside className={`sidebar ${collapsed ? 'collapsed' : 'show'}`}>
        <div className="sidebar-brand d-flex align-items-center gap-2">
          <img
            src="/images/logo-sidebar.png"
            alt="Logo"
            style={{ maxHeight: '40px', width: 'auto', borderRadius: '4px' }}
            onError={(e) => {
              (e.target as HTMLImageElement).src = 'https://picsum.photos/40/40';
            }}
          />
          {!collapsed && (
            <div className="d-flex flex-column" style={{ lineHeight: '1.2' }}>
              <span className="fw-bold tracking-wide text-white" style={{ fontSize: '0.95rem', letterSpacing: '0.5px' }}>
                Videosystem
              </span>
              <span className="fw-semibold text-white-50" style={{ fontSize: '0.75rem', letterSpacing: '0.5px' }}>
                Platform
              </span>
            </div>
          )}
        </div>

        <nav className="sidebar-nav">
          <Link to="/" className={`sidebar-link ${isActive('/')}`} title="Dashboard">
            <i className="bi bi-grid-fill"></i>
            {!collapsed && <span>Dashboard</span>}
          </Link>

          <a 
            href="https://discord.com/" 
            target="_blank" 
            rel="noopener noreferrer"
            className="sidebar-link" 
            style={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: collapsed ? 'center' : 'flex-start',
              gap: collapsed ? '0' : '0.75rem',
              padding: collapsed ? '0.75rem 0' : '0.75rem 1.5rem',
            }}
            title="Discord Chat"
          >
            <i className="bi bi-discord"></i>
            {!collapsed && (
              <span className="d-flex align-items-center justify-content-between w-100">
                <span>Discord</span>
                <span className="badge bg-warning text-dark" style={{ fontSize: '0.6rem', padding: '2px 4px', marginRight: '-5px', fontWeight: 'bold' }}>Beta</span>
              </span>
            )}
          </a>

          <a 
            href="http://192.168.1.12:3000/" 
            target="_blank" 
            rel="noopener noreferrer"
            className="sidebar-link" 
            style={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: collapsed ? 'center' : 'flex-start',
              gap: collapsed ? '0' : '0.75rem',
              padding: collapsed ? '0.75rem 0' : '0.75rem 1.5rem',
            }}
            title="Documentazione Wiki"
          >
            <i className="bi bi-book-half"></i>
            {!collapsed && (
              <span className="d-flex align-items-center justify-content-between w-100">
                <span>Wiki</span>
                <span className="badge bg-warning text-dark" style={{ fontSize: '0.6rem', padding: '2px 4px', marginRight: '-5px', fontWeight: 'bold' }}>Beta</span>
              </span>
            )}
          </a>

          <div 
            className="sidebar-link" 
            style={{ 
              opacity: 0.5, 
              cursor: 'not-allowed', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: collapsed ? 'center' : 'flex-start',
              gap: collapsed ? '0' : '0.75rem',
              padding: collapsed ? '0.75rem 0' : '0.75rem 1.5rem',
            }}
            title="Supporto Interno (Coming Soon)"
          >
            <i className="bi bi-ticket-perforated"></i>
            {!collapsed && (
              <span className="d-flex align-items-center justify-content-between w-100">
                <span>Supporto Interno</span>
                <span className="badge bg-secondary" style={{ fontSize: '0.6rem', padding: '2px 4px', marginRight: '-5px' }}>Soon</span>
              </span>
            )}
          </div>

          {user?.roles.includes('Admin') && (
            <>
              {!collapsed ? (
                <div className="px-4 py-2 mt-3 text-uppercase text-white-50 small fw-semibold" style={{ fontSize: '0.7rem', letterSpacing: '1px' }}>
                  Amministrazione
                </div>
              ) : (
                <hr className="mx-3 my-2 border-white opacity-20" />
              )}
              <Link to="/admin/users" className={`sidebar-link ${isActive('/admin/users')}`} title="Gestione Utenti">
                <i className="bi bi-people-fill"></i>
                {!collapsed && <span>Gestione Utenti</span>}
              </Link>
              <Link to="/admin/permissions" className={`sidebar-link ${isActive('/admin/permissions')}`} title="Gestione Permessi">
                <i className="bi bi-shield-lock-fill"></i>
                {!collapsed && <span>Gestione Permessi</span>}
              </Link>
            </>
          )}

          {user?.roles.includes('Developer') && (
            <>
              {!collapsed ? (
                <div className="px-4 py-2 mt-3 text-uppercase text-white-50 small fw-semibold" style={{ fontSize: '0.7rem', letterSpacing: '1px' }}>
                  Sviluppo
                </div>
              ) : (
                <hr className="mx-3 my-2 border-white opacity-20" />
              )}
              <Link to="/developer/tools" className={`sidebar-link ${isActive('/developer/tools')}`} title="Strumenti Dev">
                <i className="bi bi-bug-fill text-danger"></i>
                {!collapsed && <span>Strumenti Dev</span>}
              </Link>
            </>
          )}
        </nav>

        {/* Bottone per collassare */}
        <div className="sidebar-footer">
          <button
            onClick={toggleSidebar}
            className="collapse-toggle-btn d-flex align-items-center gap-2"
            title={collapsed ? "Espandi menu" : "Riduci menu"}
          >
            <i className={`bi ${collapsed ? 'bi-chevron-right' : 'bi-chevron-left'} fs-5`}></i>
            {!collapsed && <span style={{ fontSize: '0.9rem' }}>Riduci menu</span>}
          </button>
        </div>
      </aside>

      {/* Main Area */}
      <main className="main-content">
        <header className="topbar d-flex align-items-center justify-content-between px-4 border-bottom bg-white" style={{ height: '70px' }}>
          {/* Sezione Sinistra: Bottone Indietro + Titolo */}
          <div className="d-flex align-items-center gap-2 gap-md-3">
            {/* Hamburger menu su mobile/tablet */}
            <button
              onClick={() => setCollapsed(false)}
              className="btn btn-light d-lg-none p-2 border-0 d-flex align-items-center justify-content-center"
              style={{ width: '38px', height: '38px', borderRadius: '50%' }}
              title="Apri menu"
            >
              <i className="bi bi-list fs-4 text-dark"></i>
            </button>

            {location.pathname !== '/' && (
              <button
                onClick={handleBack}
                className="btn btn-light rounded-circle p-2 d-flex align-items-center justify-content-center border-0"
                style={{ width: '38px', height: '38px' }}
                title="Torna indietro"
              >
                <i className="bi bi-arrow-left fs-5 text-dark"></i>
              </button>
            )}
            <h5 className="mb-0 fw-bold text-dark" style={{ letterSpacing: '-0.3px', fontSize: '1.1rem' }}>
              {getPageTitle()}
            </h5>
          </div>

          {/* Sezione Centrale: Barra di Ricerca Premium */}
          <div className="d-none d-md-flex align-items-center bg-light px-3 py-2 rounded-pill border" style={{ maxWidth: '400px', width: '100%', borderColor: '#f1f5f9' }}>
            <i className="bi bi-search text-muted me-2"></i>
            <input
              type="text"
              placeholder="Cerca nella piattaforma..."
              className="form-control border-0 bg-transparent p-0 shadow-none small"
              style={{ fontSize: '0.85rem' }}
            />
          </div>

          {/* Sezione Destra: Notifiche + Profilo Dropdown */}
          <div className="d-flex align-items-center gap-3">
            {/* Campana Notifiche */}
            <div className="position-relative">
              <button
                className="btn btn-light rounded-circle p-2 border-0 d-flex align-items-center justify-content-center position-relative"
                style={{ width: '38px', height: '38px' }}
                title="Notifiche"
              >
                <i className="bi bi-bell fs-5 text-muted"></i>
                <span className="position-absolute top-0 start-100 translate-middle p-1.5 bg-danger border border-light rounded-circle" style={{ marginTop: '8px', marginRight: '8px' }}>
                  <span className="visually-hidden">Notifiche attive</span>
                </span>
              </button>
            </div>

            {/* Dropdown Account */}
            <div className="dropdown position-relative" ref={dropdownRef}>
              <button
                className="btn btn-light border-0 d-flex align-items-center gap-2 py-1 px-2.5 rounded-pill"
                style={{ borderRadius: '20px' }}
                onClick={() => setDropdownOpen(!dropdownOpen)}
              >
                <div
                  className="avatar-circle text-white fw-bold d-flex align-items-center justify-content-center"
                  style={{
                    width: '30px',
                    height: '30px',
                    borderRadius: '50%',
                    background: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)',
                    fontSize: '0.8rem'
                  }}
                >
                  {user?.fullName?.substring(0, 2).toUpperCase() || user?.username?.substring(0, 2).toUpperCase() || 'US'}
                </div>
                <div className="text-start d-none d-lg-block" style={{ lineHeight: '1.2' }}>
                  <div className="fw-semibold text-dark small">{user?.fullName || user?.username}</div>
                  <div className="text-muted" style={{ fontSize: '0.65rem' }}>{user?.roles[0] || 'User'}</div>
                </div>
                <i className={`bi ${dropdownOpen ? 'bi-chevron-up' : 'bi-chevron-down'} text-muted small`}></i>
              </button>

              {dropdownOpen && (
                <div
                  className="position-absolute end-0 bg-white shadow-lg border p-3 mt-2 rounded-3 animate-fade-in"
                  style={{ width: '280px', zIndex: 1050, border: '1px solid #f1f5f9' }}
                >
                  <div className="text-center pb-3 border-bottom mb-2">
                    <div
                      className="avatar-circle mx-auto text-white fw-bold d-flex align-items-center justify-content-center mb-2"
                      style={{
                        width: '52px',
                        height: '52px',
                        borderRadius: '50%',
                        background: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)',
                        fontSize: '1.2rem'
                      }}
                    >
                      {user?.fullName?.substring(0, 2).toUpperCase() || user?.username?.substring(0, 2).toUpperCase() || 'US'}
                    </div>
                    <h6 className="fw-bold text-dark mb-0">{user?.fullName || user?.username}</h6>
                    <span className="small text-muted d-block">{user?.email}</span>
                  </div>

                  <div className="py-1">
                    <div className="d-flex justify-content-between align-items-center py-1 small">
                      <span className="text-muted">Username:</span>
                      <span className="fw-semibold text-dark">{user?.username}</span>
                    </div>
                    <div className="d-flex justify-content-between align-items-center py-1 small">
                      <span className="text-muted">Reparto / Ruolo:</span>
                      <span className="badge bg-secondary text-capitalize">{user?.roles.join(', ')}</span>
                    </div>
                    <div className="d-flex justify-content-between align-items-center py-1 small">
                      <span className="text-muted">Connessione:</span>
                      <span className="text-success fw-semibold">Sicura</span>
                    </div>
                  </div>

                  <div className="border-top pt-2 mt-2">
                    <button
                      onClick={handleLogoutClick}
                      className="btn btn-outline-danger btn-sm w-100 d-flex align-items-center justify-content-center gap-2 fw-semibold"
                      style={{ borderRadius: '6px' }}
                    >
                      <i className="bi bi-box-arrow-right"></i> Disconnetti Account
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </header>

        <div className="page-body animate-fade-in">
          {children}
        </div>
      </main>
    </div>
  );
}
