import React, { useState } from 'react';

export default function DeveloperTools() {
  const [devMode, setDevMode] = useState(() => document.cookie.includes('dev_mode=true'));

  const toggleDevMode = (e: React.ChangeEvent<HTMLInputElement>) => {
    const active = e.target.checked;
    setDevMode(active);
    if (active) {
      document.cookie = "dev_mode=true; path=/; max-age=86400; Secure; SameSite=Strict";
    } else {
      document.cookie = "dev_mode=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
    }
    window.location.reload();
  };

  return (
    <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
      <h5 className="fw-bold mb-3 text-dark d-flex align-items-center gap-2">
        <i className="bi bi-bug-fill text-danger"></i> Strumenti di Sviluppo
      </h5>
      <p className="text-muted small mb-4">
        Da questa pagina gli utenti con ruolo sviluppatore possono abilitare o disabilitare la modalità di bypass dei permessi.
        La Dev Mode consente l'accesso completo a tutte le funzioni e le applicazioni del portale per agevolare il testing e lo sviluppo in locale.
      </p>

      <div className="card border-danger p-3 bg-light" style={{ borderRadius: '8px' }}>
        <div className="form-check form-switch d-flex align-items-center gap-3">
          <input 
            className="form-check-input" 
            type="checkbox" 
            id="pageDevSwitch" 
            checked={devMode}
            onChange={toggleDevMode}
            style={{ width: '2.5rem', height: '1.25rem', cursor: 'pointer' }}
          />
          <div>
            <label className="form-check-label fw-bold text-danger mb-0" htmlFor="pageDevSwitch" style={{ cursor: 'pointer', userSelect: 'none' }}>
              Abilita Bypass Autorizzazioni (Dev Mode)
            </label>
            <div className="small text-muted">
              {devMode 
                ? 'Attivo - Stai visualizzando e modificando il portale con privilegi di debug illimitati.' 
                : 'Disattivo - I permessi applicativi vengono valutati regolarmente a cascata.'}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
