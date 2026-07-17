import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';

export default function SetPassword() {
  const [searchParams] = useSearchParams();

  const userId = searchParams.get('userId') || '';
  const token = searchParams.get('token') || '';

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password.length < 6) {
      setError('La password deve contenere almeno 6 caratteri.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Le password inserite non coincidono.');
      return;
    }

    setLoading(true);

    try {
      const response = await fetch('/Account/SetPassword', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userId,
          token,
          password,
          confirmPassword,
        }),
      });

      if (response.ok) {
        setSuccess(true);
        setTimeout(() => {
          window.location.href = '/';
        }, 2500);
      } else {
        const data = await response.json();
        setError(data.message || 'Errore durante la configurazione della password.');
      }
    } catch (err) {
      console.error(err);
      setError('Errore di connessione. Riprova più tardi.');
    } finally {
      setLoading(false);
    }
  };

  if (!userId || !token) {
    return (
      <div className="container-fluid d-flex align-items-center justify-content-center" style={{ minHeight: '100vh', background: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)' }}>
        <div className="card shadow-lg border-0 p-4" style={{ width: '100%', maxWidth: '420px', borderRadius: '16px' }}>
          <div className="alert alert-danger text-center mb-0">
            <i className="bi bi-exclamation-triangle-fill fs-3 d-block mb-2"></i>
            Link di configurazione non valido o scaduto. Contatta l'amministratore per farti inviare un nuovo invito.
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container-fluid d-flex align-items-center justify-content-center" style={{ minHeight: '100vh', background: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)' }}>
      <div className="card shadow-lg border-0 p-4" style={{ width: '100%', maxWidth: '420px', borderRadius: '16px', backgroundColor: 'rgba(255, 255, 255, 0.95)' }}>
        
        <div className="text-center mb-4">
          <img src="/images/logo-azienda.png" alt="Logo Azienda" style={{ maxHeight: '60px', width: 'auto' }} onError={(e) => {
            (e.target as HTMLImageElement).src = 'https://picsum.photos/150/60';
          }} />
          <h4 className="fw-bold mt-3 text-dark">Imposta la tua Password</h4>
          <p className="text-muted small">Inserisci una password sicura per il tuo account aziendale</p>
        </div>

        {error && (
          <div className="alert alert-danger py-2 small" role="alert">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            {error}
          </div>
        )}

        {success ? (
          <div className="alert alert-success py-3 text-center mb-0" role="alert">
            <i className="bi bi-check-circle-fill fs-4 d-block mb-2"></i>
            Password salvata con successo! <br />
            <span className="small text-muted">Verrai reindirizzato alla pagina di login...</span>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">Nuova Password</label>
              <div className="input-group">
                <span className="input-group-text bg-white border-end-0 text-muted">
                  <i className="bi bi-lock"></i>
                </span>
                <input
                  type="password"
                  className="form-control border-start-0 ps-0"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Almeno 6 caratteri"
                  required
                  disabled={loading}
                />
              </div>
            </div>

            <div className="mb-4">
              <label className="form-label small fw-semibold text-muted">Conferma Password</label>
              <div className="input-group">
                <span className="input-group-text bg-white border-end-0 text-muted">
                  <i className="bi bi-lock-fill"></i>
                </span>
                <input
                  type="password"
                  className="form-control border-start-0 ps-0"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="Ripeti la password"
                  required
                  disabled={loading}
                />
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
              CONFERMA PASSWORD
            </button>
          </form>
        )}

        <div className="text-center mt-4 pt-3 border-top text-muted" style={{ fontSize: '0.75rem' }}>
          © {new Date().getFullYear()} Videosystem S.r.l.
        </div>

      </div>
    </div>
  );
}
