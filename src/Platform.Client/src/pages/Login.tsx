import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';

interface LoginProps {
  onLoginSuccess: (user: any) => void;
}

export default function Login({ onLoginSuccess }: LoginProps) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  
  const [searchParams] = useSearchParams();

  useEffect(() => {
    const errorParam = searchParams.get('error');
    if (errorParam) {
      setError(decodeURIComponent(errorParam));
    }
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const response = await fetch('/Account/Login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username,
          password,
          rememberMe,
        }),
      });

      if (response.ok) {
        const data = await response.json();
        onLoginSuccess(data.user);
      } else {
        const data = await response.json();
        setError(data.message || 'Credenziali non valide o errore di connessione.');
      }
    } catch (err) {
      setError('Errore di rete. Impossibile connettersi al server.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };



  return (
    <div className="container-fluid d-flex align-items-center justify-content-center" style={{ minHeight: '100vh', background: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)' }}>
      <div className="card shadow-lg border-0 p-4 animate-fade-in" style={{ width: '100%', maxWidth: '420px', borderRadius: '16px', backgroundColor: 'rgba(255, 255, 255, 0.95)' }}>
        
        <div className="text-center mb-4">
          <img src="/images/logo-azienda.png" alt="Logo Azienda" style={{ maxHeight: '60px', width: 'auto' }} onError={(e) => {
            (e.target as HTMLImageElement).src = 'https://picsum.photos/150/60';
          }} />
          <h4 className="fw-bold mt-3 text-dark">Accedi alla Piattaforma</h4>
          <p className="text-muted small">Inserisci le tue credenziali aziendali</p>
        </div>

        {error && (
          <div className="alert alert-danger py-2 small" role="alert">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label small fw-semibold text-muted">Username o Email</label>
            <div className="input-group">
              <span className="input-group-text bg-white border-end-0 text-muted">
                <i className="bi bi-person"></i>
              </span>
              <input
                type="text"
                className="form-control border-start-0 ps-0"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="nome.cognome"
                required
                disabled={loading}
              />
            </div>
          </div>

          <div className="mb-3">
            <label className="form-label small fw-semibold text-muted">Password</label>
            <div className="input-group">
              <span className="input-group-text bg-white border-end-0 text-muted">
                <i className="bi bi-lock"></i>
              </span>
              <input
                type="password"
                className="form-control border-start-0 ps-0"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                disabled={loading}
              />
            </div>
          </div>

          <div className="mb-3 d-flex align-items-center justify-content-between">
            <div className="form-check">
              <input
                type="checkbox"
                className="form-check-input"
                id="rememberMe"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                disabled={loading}
              />
              <label className="form-check-label small text-muted user-select-none" htmlFor="rememberMe">Ricordami</label>
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary w-100 py-2 fw-semibold"
            disabled={loading}
            style={{ borderRadius: '8px' }}
          >
            {loading ? (
              <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
            ) : null}
            ACCEDI
          </button>
        </form>



        <div className="text-center mt-4 pt-3 border-top text-muted" style={{ fontSize: '0.75rem' }}>
          © {new Date().getFullYear()} Videosystem S.r.l.
        </div>

      </div>
    </div>
  );
}
