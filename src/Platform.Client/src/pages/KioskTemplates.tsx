import { useEffect, useState } from 'react';

interface TemplateItem {
  id: number;
  name: string;
  version: string;
  description: string;
  isActive: boolean;
  structureJson: string;
  createdAt: string;
}

interface KioskTemplatesProps {
  companyId: 'Pharmaself24' | 'Skriptkiosk';
}

export default function KioskTemplates({ companyId }: KioskTemplatesProps) {
  const [templates, setTemplates] = useState<TemplateItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Form states
  const [showModal, setShowModal] = useState(false);
  const [name, setName] = useState('');
  const [version, setVersion] = useState('1.0');
  const [description, setDescription] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [structureJson, setStructureJson] = useState('');
  const [fileError, setFileError] = useState<string | null>(null);
  
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const apiPath = companyId === 'Skriptkiosk' ? '/api/SkriptKioskAdmin' : '/api/KioskAdmin';

  const fetchTemplates = () => {
    fetch(apiPath)
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento dei template.');
        return res.json();
      })
      .then((data) => {
        setTemplates(data);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setError('Impossibile caricare i template.');
        setLoading(false);
      });
  };

  useEffect(() => {
    fetchTemplates();
  }, []);

  const handleOpenCreate = () => {
    setName('');
    setVersion('1.0');
    setDescription('');
    setIsActive(true);
    setStructureJson('');
    setFileError(null);
    setFormError(null);
    setFormSuccess(null);
    setShowModal(true);
  };

  // Client-side JSON file reader
  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFileError(null);
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target?.result as string;
      try {
        // Valida la correttezza del JSON sul client
        JSON.parse(text);
        setStructureJson(text);
      } catch (err) {
        setFileError('Il file selezionato non contiene un JSON valido.');
        console.error(err);
      }
    };
    reader.readAsText(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(null);

    if (!structureJson.trim()) {
      setFormError('Devi caricare un file JSON o compilare la struttura del template.');
      return;
    }

    setSubmitting(true);
    try {
      const response = await fetch(apiPath, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name,
          version: parseInt(version, 10) || 1,
          description,
          isActive,
          structureJson,
        }),
      });

      const resData = await response.json();

      if (response.ok) {
        setFormSuccess(resData.message || 'Template creato con successo!');
        fetchTemplates();
        setTimeout(() => setShowModal(false), 1500);
      } else {
        setFormError(resData.message || 'Errore durante la creazione del template.');
      }
    } catch (err) {
      console.error(err);
      setFormError('Errore di connessione con il server.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleStatus = async (id: number) => {
    try {
      const response = await fetch(`${apiPath}/${id}/toggle-status`, {
        method: 'POST',
      });
      if (response.ok) {
        fetchTemplates();
      } else {
        alert('Impossibile aggiornare lo stato del template.');
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (!window.confirm(`Sei sicuro di voler eliminare definitivamente il template "${name}"?`)) return;

    try {
      const response = await fetch(`${apiPath}/${id}`, {
        method: 'DELETE',
      });

      const data = await response.json();
      if (response.ok) {
        alert(data.message || 'Template eliminato.');
        fetchTemplates();
      } else {
        alert(data.message || 'Errore nell\'eliminazione del template.');
      }
    } catch (err) {
      console.error(err);
    }
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

  return (
    <div>
      <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
        <div className="d-flex justify-content-between align-items-center mb-4">
          <h5 className="fw-bold mb-0 text-dark">Gestione Template Checklist</h5>
          <button onClick={handleOpenCreate} className="btn btn-primary d-flex align-items-center gap-1">
            <i className="bi bi-plus-circle-fill"></i> Nuovo Template
          </button>
        </div>

        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead>
              <tr className="text-secondary small">
                <th>Nome Template</th>
                <th>Versione</th>
                <th>Descrizione</th>
                <th>Stato</th>
                <th>Data Creazione</th>
                <th className="text-end">Azioni</th>
              </tr>
            </thead>
            <tbody>
              {templates.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">Nessun template configurato.</td>
                </tr>
              ) : (
                templates.map((t) => (
                  <tr key={t.id}>
                    <td>
                      <div className="fw-bold text-dark">{t.name}</div>
                    </td>
                    <td className="small text-muted">{t.version}</td>
                    <td className="small text-muted" style={{ maxWidth: '280px', textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                      {t.description || 'Nessuna descrizione'}
                    </td>
                    <td>
                      <div className="form-check form-switch">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          checked={t.isActive}
                          onChange={() => handleToggleStatus(t.id)}
                          style={{ cursor: 'pointer' }}
                        />
                        <span className={`small ms-1 ${t.isActive ? 'text-success' : 'text-danger'}`}>
                          {t.isActive ? 'Attivo' : 'Inattivo'}
                        </span>
                      </div>
                    </td>
                    <td className="small text-muted">{new Date(t.createdAt).toLocaleDateString()}</td>
                    <td className="text-end">
                      <button 
                        onClick={() => handleDelete(t.id, t.name)}
                        className="btn btn-sm btn-outline-danger"
                        title="Elimina"
                      >
                        <i className="bi bi-trash"></i>
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Creazione */}
      {showModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">Crea Nuovo Template</h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>
              
              <form onSubmit={handleSubmit}>
                <div className="modal-body py-3">
                  {formError && <div className="alert alert-danger py-2 small mb-3">{formError}</div>}
                  {formSuccess && <div className="alert alert-success py-2 small mb-3">{formSuccess}</div>}

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-muted">Nome Template</label>
                    <input
                      type="text"
                      className="form-control"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      placeholder="Checklist Installazione Kiosk"
                      required
                    />
                  </div>

                  <div className="row g-3 mb-3">
                    <div className="col-6">
                      <label className="form-label small fw-semibold text-muted">Versione</label>
                      <input
                        type="text"
                        className="form-control"
                        value={version}
                        onChange={(e) => setVersion(e.target.value)}
                        placeholder="1.0"
                        required
                      />
                    </div>
                    
                    <div className="col-6 d-flex align-items-end pb-2">
                      <div className="form-check form-switch mb-1">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          id="modalIsActive"
                          checked={isActive}
                          onChange={(e) => setIsActive(e.target.checked)}
                        />
                        <label className="form-check-label small text-muted fw-semibold" htmlFor="modalIsActive">Attivo</label>
                      </div>
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-muted">Descrizione</label>
                    <textarea
                      className="form-control"
                      rows={2}
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
                      placeholder="Descrivi a cosa serve questo template..."
                    />
                  </div>

                  <div className="mb-2">
                    <label className="form-label small fw-semibold text-muted">Carica File Struttura (JSON)</label>
                    <input
                      type="file"
                      className="form-control"
                      accept=".json"
                      onChange={handleFileUpload}
                      required
                    />
                    {fileError && <div className="text-danger small mt-1">{fileError}</div>}
                    {!fileError && structureJson && (
                      <div className="text-success small mt-1">
                        <i className="bi bi-check-circle-fill me-1"></i> Struttura JSON caricata e validata.
                      </div>
                    )}
                  </div>
                </div>

                <div className="modal-footer border-0 pt-0">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Annulla
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={submitting || !!fileError}>
                    {submitting ? <span className="spinner-border spinner-border-sm me-1"></span> : null}
                    Crea Template
                  </button>
                </div>
              </form>

            </div>
          </div>
        </div>
      )}
    </div>
  );
}
