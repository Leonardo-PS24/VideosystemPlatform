import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

interface ApplicationInfo {
  appId: string;
  title?: string;
  name?: string; 
  description: string;
  url: string;
  icon: string;
  companyId: string;
  comingSoon?: boolean;
}

interface CompanyInfo {
  id: string;
  name: string;
  primaryColor: string;
  secondaryColor: string;
  applications: ApplicationInfo[];
}

interface UserData {
  username: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface DashboardProps {
  user: UserData | null;
}

export default function Dashboard({ user }: DashboardProps) {
  const [companies, setCompanies] = useState<CompanyInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [currentTime, setCurrentTime] = useState('--:--');
  const navigate = useNavigate();

  // Clock update effect
  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      const hours = String(now.getHours()).padStart(2, '0');
      const minutes = String(now.getMinutes()).padStart(2, '0');
      setCurrentTime(`${hours}:${minutes}`);
    };
    
    updateTime();
    const timer = setInterval(updateTime, 60000);
    return () => clearInterval(timer);
  }, []);

  // Fetch data
  useEffect(() => {
    fetch('/api/Home')
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento dei dati del dashboard.');
        return res.json();
      })
      .then((data) => {
        setCompanies(data);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setError('Impossibile caricare le applicazioni disponibili.');
        setLoading(false);
      });
  }, []);

  const handleAppLaunch = (app: ApplicationInfo) => {
    if (app.comingSoon) return;
    if (app.appId === 'ConfigurationKiosk' || app.appId === 'kiosk') {
      navigate('/kiosk');
    } else {
      window.open(app.url, '_blank');
    }
  };

  const getTodayDateString = () => {
    return new Date().toLocaleDateString('it-IT', {
      day: '2-digit',
      month: 'long',
      year: 'numeric'
    });
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '300px' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Caricamento...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        <i className="bi bi-exclamation-triangle-fill me-2"></i>
        {error}
      </div>
    );
  }

  const isAdmin = user?.roles.includes('Admin');

  return (
    <div>
      {/* Statistiche Superiori */}
      <div className="row mb-4 g-3">
        {isAdmin ? (
          <>
            <div className="col-12 col-md-4">
              <div className="card stat-card shadow-sm border-0">
                <div className="stat-icon primary">
                  <span className="material-icons">business</span>
                </div>
                <div className="stat-info">
                  <h3>{companies.length}</h3>
                  <p>Aziende Gestite</p>
                </div>
              </div>
            </div>
            
            <div className="col-12 col-md-4">
              <div className="card stat-card shadow-sm border-0">
                <div className="stat-icon primary">
                  <span className="material-icons">schedule</span>
                </div>
                <div className="stat-info">
                  <h3>{currentTime}</h3>
                  <p>Ora Sistema</p>
                </div>
              </div>
            </div>

            <div className="col-12 col-md-4">
              <div className="card stat-card shadow-sm border-0">
                <div className="stat-icon primary">
                  <span className="material-icons">dns</span>
                </div>
                <div className="stat-info">
                  <h3>Online</h3>
                  <p>Stato Sistema</p>
                </div>
              </div>
            </div>
          </>
        ) : (
          <>
            <div className="col-12 col-md-6">
              <div className="card stat-card shadow-sm border-0">
                <div className="stat-icon primary">
                  <span className="material-icons">waving_hand</span>
                </div>
                <div className="stat-info">
                  <h3>Ciao, {user?.fullName || user?.username}</h3>
                  <p>Benvenuto nella tua dashboard</p>
                </div>
              </div>
            </div>
            
            <div className="col-12 col-md-6">
              <div className="card stat-card shadow-sm border-0">
                <div className="stat-icon primary">
                  <span className="material-icons">calendar_today</span>
                </div>
                <div className="stat-info">
                  <h3>{getTodayDateString()}</h3>
                  <p>Data Odierna</p>
                </div>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Aziende e Applicazioni */}
      {companies.length === 0 ? (
        <div className="card p-5 text-center border-0 shadow-sm bg-white" style={{ borderRadius: '12px' }}>
          <div className="mb-3 text-muted">
            <span className="material-icons" style={{ fontSize: '48px' }}>business</span>
          </div>
          <h5>Nessuna azienda configurata</h5>
          <p className="text-muted">Non sono state ancora configurate aziende nella piattaforma.</p>
        </div>
      ) : (
        companies.map((company) => (
          <div key={company.id} className="card border-0 shadow-sm mb-4 overflow-hidden" style={{ borderRadius: '12px' }}>
            <div 
              className="card-header border-0 py-3" 
              style={{ backgroundColor: company.primaryColor, color: company.secondaryColor }}
            >
              <h5 className="mb-0 fw-bold">{company.name}</h5>
            </div>
            
            <div className="card-body p-4">
              {company.applications.length === 0 ? (
                <p className="text-muted small mb-0">Nessuna applicazione disponibile per questa azienda.</p>
              ) : (
                <div className="row g-4">
                  {company.applications.map((app) => {
                    const appTitle = app.title || app.name || 'Applicazione';
                    const isComingSoon = app.comingSoon;
                    return (
                      <div key={app.appId} className="col-12 col-md-6 col-lg-4">
                        <div 
                          onClick={() => handleAppLaunch(app)}
                          className="card app-card position-relative"
                          style={isComingSoon ? { 
                            opacity: 0.65, 
                            cursor: 'not-allowed',
                            pointerEvents: 'initial',
                            backgroundColor: '#f8fafc'
                          } : {}}
                        >
                          {isComingSoon && (
                            <span 
                              className="position-absolute badge bg-secondary text-uppercase" 
                              style={{ 
                                top: '12px', 
                                right: '12px', 
                                fontSize: '0.65rem',
                                letterSpacing: '0.5px',
                                padding: '4px 8px',
                                borderRadius: '4px'
                              }}
                            >
                              Coming Soon
                            </span>
                          )}
                          <div className="card-body text-center d-flex flex-column align-items-center h-100 p-4">
                            <div 
                              className="app-icon-container" 
                              style={{ 
                                backgroundColor: isComingSoon ? '#e2e8f0' : `${company.primaryColor}1A`, 
                                color: isComingSoon ? '#64748b' : company.primaryColor 
                              }}
                            >
                              <span className="material-icons">{app.icon || 'apps'}</span>
                            </div>
                            <h5 className="app-title">{appTitle}</h5>
                            <p className="app-desc mb-4">{app.description}</p>
                            <span 
                              className="btn btn-sm mt-auto w-100 fw-semibold" 
                              style={isComingSoon ? {
                                borderColor: '#cbd5e1',
                                color: '#94a3b8',
                                borderStyle: 'solid',
                                borderWidth: '1px',
                                backgroundColor: '#f1f5f9'
                              } : { 
                                borderColor: company.primaryColor, 
                                color: company.primaryColor,
                                borderStyle: 'solid',
                                borderWidth: '1px'
                              }}
                            >
                              {isComingSoon ? 'Coming Soon' : 'Apri'}
                            </span>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        ))
      )}
    </div>
  );
}
