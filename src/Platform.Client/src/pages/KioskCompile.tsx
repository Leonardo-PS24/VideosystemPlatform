import { useEffect, useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { HubConnectionBuilder } from '@microsoft/signalr';

interface Field {
  id: string;
  type: 'boolean' | 'toggle' | 'checkbox' | 'text' | 'textarea' | 'number' | 'select' | 'file' | 'signature' | 'date' | 'link';
  label: string;
  description?: string;
  required?: boolean;
  options?: any[];
  url?: string;
  placeholder?: string;
  button_text?: string;
}

interface Section {
  id: string;
  title: string;
  description?: string;
  fields: Field[];
}

interface TemplateStructure {
  sections: Section[];
}

interface InstanceData {
  status: 'InProgress' | 'Completed' | 'InRevision' | 'Finalized';
  machineSerialNumber: string;
  progress: number;
  revision: number;
  dataJson: string;
  template: {
    name: string;
    structureJson: string;
  };
}

export default function KioskCompile() {
  const { id } = useParams<{ id: string }>();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [canEdit, setCanEdit] = useState(false);
  const [socketLogs, setSocketLogs] = useState<string[]>([]);
  
  const addLog = (msg: string) => {
    setSocketLogs(prev => [`${new Date().toLocaleTimeString()} - ${msg}`, ...prev.slice(0, 49)]);
  };
  
  // Instance & Template structures
  const [instance, setInstance] = useState<InstanceData | null>(null);
  const [structure, setStructure] = useState<TemplateStructure | null>(null);
  const [activeSectionId, setActiveSectionId] = useState<string>('');
  
  // Form states
  const [answers, setAnswers] = useState<Record<string, any>>({});
  const [touchedFields, setTouchedFields] = useState<string[]>([]);
  const [saveStatus, setSaveStatus] = useState<'saved' | 'saving' | 'error'>('saved');

  // Refs for debouncing
  const saveTimeoutRef = useRef<any>(null);
  const answersRef = useRef(answers);
  const touchedRef = useRef(touchedFields);

  // Keep refs in sync for use in debounce and events
  useEffect(() => {
    answersRef.current = answers;
    touchedRef.current = touchedFields;
  }, [answers, touchedFields]);

  // Load Kiosk data from API
  const loadKioskData = async (showLoading = true) => {
    if (showLoading) setLoading(true);
    try {
      const response = await fetch(`/api/Kiosk/${id}`);
      if (!response.ok) throw new Error("Errore nel recupero della checklist.");
      const json = await response.json();
      
      setCanEdit(json.canEdit);
      setInstance(json.instance);

      // Parse template structure
      const parsedStructure: TemplateStructure = JSON.parse(json.template.structureJson || '{"sections":[]}');
      setStructure(parsedStructure);
      if (parsedStructure.sections.length > 0 && !activeSectionId) {
        setActiveSectionId(parsedStructure.sections[0].id);
      }

      // Parse current answers
      const parsedAnswers = JSON.parse(json.instance.dataJson || '{}');
      setAnswers(parsedAnswers);
      setTouchedFields(parsedAnswers._touched || []);

      setLoading(false);
    } catch (err) {
      console.error(err);
      setError("Impossibile caricare l'istanza della checklist.");
      setLoading(false);
    }
  };

  useEffect(() => {
    loadKioskData();

    // Setup SignalR connection
    const connection = new HubConnectionBuilder()
      .withUrl('/kioskhub')
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => {
        addLog("SignalR Connected");
        connection.invoke('JoinChecklistGroup', id);
        addLog(`Joined checklist group ${id}`);
      })
      .catch(err => {
        console.error("SignalR Connection Error:", err);
        addLog(`SignalR Connection Error: ${err.message || err}`);
      });

    // Handle updates from other clients
    connection.on('DataUpdated', (userId) => {
      addLog(`Received DataUpdated event from user: ${userId || 'Operator'}`);
      loadKioskData(false);
    });

    connection.on('UpdateProgress', (instanceId, progress) => {
      if (Number(instanceId) === Number(id)) {
        addLog(`Ricevuto aggiornamento progresso: ${progress}%`);
        loadKioskData(false);
      }
    });

    connection.on('InstanceCompleted', (instanceId) => {
      if (Number(instanceId) === Number(id)) {
        addLog("Ricevuto evento di completamento checklist.");
        loadKioskData(false);
      }
    });

    connection.on('UpdateStatus', (instanceId, newStatus) => {
      if (Number(instanceId) === Number(id)) {
        addLog(`Stato checklist modificato in: ${newStatus}`);
        loadKioskData(false);
      }
    });

    connection.on('RevisionFinalized', (instanceId) => {
      if (Number(instanceId) === Number(id)) {
        addLog("Revisione checklist finalizzata.");
        loadKioskData(false);
      }
    });

    connection.onreconnecting((error) => {
      addLog(`SignalR Reconnecting: ${error?.message || error}`);
    });

    connection.onreconnected((connectionId) => {
      addLog(`SignalR Reconnected: ${connectionId}`);
    });

    return () => {
      if (connection) {
        connection.invoke('LeaveChecklistGroup', id).catch(() => {});
        connection.off('DataUpdated');
        connection.off('UpdateProgress');
        connection.off('InstanceCompleted');
        connection.off('UpdateStatus');
        connection.off('RevisionFinalized');
        connection.stop().catch(() => {});
      }
    };
  }, [id]);

  // Count total interactive fields
  const getTotalFields = (): { total: number; requiredKeys: string[] } => {
    if (!structure) return { total: 0, requiredKeys: [] };
    let total = 0;
    const requiredKeys: string[] = [];
    structure.sections.forEach(s => {
      s.fields.forEach(f => {
        if (f.type !== 'link') {
          total++;
          if (f.required) {
            requiredKeys.push(f.id);
          }
        }
      });
    });
    return { total, requiredKeys };
  };

  // Handle answers input changes
  const handleInputChange = (fieldId: string, value: any) => {
    if (!canEdit || instance?.status === 'Completed' || instance?.status === 'Finalized') return;

    // Aggiorna stato locale risposte
    const updatedAnswers = { ...answers, [fieldId]: value };
    setAnswers(updatedAnswers);

    // Aggiorna elenco campi toccati (per il progresso)
    if (!touchedFields.includes(fieldId)) {
      const updatedTouched = [...touchedFields, fieldId];
      setTouchedFields(updatedTouched);
      debouncedSave(updatedAnswers, updatedTouched);
    } else {
      debouncedSave(updatedAnswers, touchedFields);
    }
  };

  // Debounced auto-save
  const debouncedSave = (currAnswers: Record<string, any>, currTouched: string[]) => {
    setSaveStatus('saving');
    if (saveTimeoutRef.current) {
      clearTimeout(saveTimeoutRef.current);
    }

    saveTimeoutRef.current = setTimeout(async () => {
      // Calcola progresso
      const { total } = getTotalFields();
      const progressPercent = total > 0 ? Math.round((currTouched.length / total) * 100) : 0;

      const payload = {
        instanceId: Number(id),
        dataJson: JSON.stringify({
          ...currAnswers,
          _touched: currTouched,
          _progressPercent: progressPercent
        })
      };

      try {
        const response = await fetch('/api/Kiosk/save', {
          method: 'PATCH',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(payload)
        });

        if (response.ok) {
          setSaveStatus('saved');
        } else {
          setSaveStatus('error');
        }
      } catch (err) {
        console.error(err);
        setSaveStatus('error');
      }
    }, 1000); // 1 secondo debounce
  };

  // Complete Checklist click
  const handleComplete = async () => {
    const { requiredKeys } = getTotalFields();
    
    // Validazione campi obbligatori
    const missing: string[] = [];
    requiredKeys.forEach(k => {
      if (answers[k] === undefined || answers[k] === null || answers[k] === '') {
        missing.push(k);
      }
    });

    if (missing.length > 0) {
      alert("Attenzione: Ci sono campi obbligatori non ancora compilati!");
      return;
    }

    if (!window.confirm("Sei sicuro di voler completare e bloccare questa configurazione? Non sarà più modificabile senza revisione.")) return;

    try {
      const { total } = getTotalFields();
      const progressPercent = total > 0 ? Math.round((touchedFields.length / total) * 100) : 100;

      const payload = {
        instanceId: Number(id),
        dataJson: JSON.stringify({
          ...answers,
          _touched: touchedFields,
          _progressPercent: progressPercent
        })
      };

      const response = await fetch('/api/Kiosk/complete', {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        loadKioskData();
      } else {
        alert("Errore nel completamento.");
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleStartRevision = async () => {
    if (!window.confirm("Vuoi avviare la revisione di questa checklist? Diventerà modificabile.")) return;
    try {
      const response = await fetch('/api/Kiosk/start-revision', {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ instanceId: Number(id) })
      });
      if (response.ok) {
        loadKioskData();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleFinalizeRevision = async () => {
    if (!window.confirm("Vuoi finalizzare la revisione e congelare la configurazione?")) return;
    try {
      const payload = {
        instanceId: Number(id),
        dataJson: JSON.stringify({
          ...answers,
          _touched: touchedFields,
          _progressPercent: 100
        })
      };

      const response = await fetch('/api/Kiosk/finalize-revision', {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });
      
      if (response.ok) {
        const result = await response.json();
        if (result.changesDetected) {
          alert("Revisione completata con successo! Rilevate modifiche rispetto alla versione precedente.");
        } else {
          alert("Revisione completata. Nessuna modifica rilevata rispetto alla versione precedente.");
        }
        loadKioskData();
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

  if (error || !instance || !structure) {
    return (
      <div className="alert alert-danger" role="alert">
        <i className="bi bi-exclamation-triangle-fill me-2"></i>
        {error}
      </div>
    );
  }

  const { total } = getTotalFields();
  const currentProgress = total > 0 ? Math.round((touchedFields.length / total) * 100) : 0;
  const isLocked = instance.status === 'Completed' || instance.status === 'Finalized';

  return (
    <div>
      {/* Header Info */}
      <div className="card border-0 shadow-sm p-3 mb-4 bg-white" style={{ borderRadius: '12px' }}>
        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3">
          <div>
            <h6 className="text-uppercase text-muted small fw-bold mb-1">Checklist Macchina</h6>
            <h4 className="fw-bold mb-0 text-dark">
              {instance.template.name} <span className="text-secondary small fw-normal">Matricola: {instance.machineSerialNumber}</span>
            </h4>
          </div>

          <div className="d-flex align-items-center gap-3">
            {/* Indicatore di salvataggio */}
            {!isLocked && (
              <span className="small">
                {saveStatus === 'saving' && <span className="text-primary"><span className="spinner-border spinner-border-sm me-1"></span> Salvataggio...</span>}
                {saveStatus === 'saved' && <span className="text-success"><i className="bi bi-cloud-check-fill me-1"></i> Salvato</span>}
                {saveStatus === 'error' && <span className="text-danger"><i className="bi bi-cloud-slash-fill me-1"></i> Errore di salvataggio</span>}
              </span>
            )}

            <div className="text-end">
              <span className="small text-muted d-block">Stato: <strong className="text-dark">{instance.status}</strong></span>
              <span className="small text-muted d-block">Revisione: <strong className="text-dark">v{instance.revision}</strong></span>
            </div>

            <div className="text-center bg-light rounded-3 px-3 py-2" style={{ minWidth: '90px' }}>
              <span className="d-block small text-muted">Progresso</span>
              <strong className="fs-5 text-primary">{currentProgress}%</strong>
            </div>
          </div>
        </div>
      </div>

      <div className="row g-4">
        {/* Navigazione Sezioni */}
        <div className="col-12 col-md-4 col-lg-3">
          <div className="card border-0 shadow-sm bg-white overflow-hidden" style={{ borderRadius: '12px' }}>
            <div className="p-3 bg-light border-bottom fw-bold text-dark small text-uppercase tracking-wider">
              Sezioni Checklist
            </div>
            
            <div className="list-group list-group-flush">
              {structure.sections.map((sec) => (
                <button
                  key={sec.id}
                  onClick={() => setActiveSectionId(sec.id)}
                  className={`list-group-item list-group-item-action border-0 py-3 d-flex align-items-center justify-content-between small fw-semibold ${activeSectionId === sec.id ? 'bg-primary text-white' : 'text-dark'}`}
                >
                  <span>{sec.title}</span>
                  <i className={`bi ${activeSectionId === sec.id ? 'bi-chevron-right' : 'bi-chevron-left-short text-muted'}`}></i>
                </button>
              ))}
            </div>
          </div>

          {/* Azioni di stato compilazione */}
          <div className="mt-3">
            {!isLocked && canEdit && (
              <button 
                onClick={handleComplete}
                className="btn btn-success w-100 py-3 fw-bold shadow-sm d-flex align-items-center justify-content-center gap-2"
                style={{ borderRadius: '12px' }}
              >
                <i className="bi bi-check-circle-fill"></i>
                Completa Configurazione
              </button>
            )}

            {instance.status === 'Completed' && (
              <button 
                onClick={handleStartRevision}
                className="btn btn-warning w-100 py-3 fw-bold text-dark shadow-sm d-flex align-items-center justify-content-center gap-2"
                style={{ borderRadius: '12px' }}
              >
                <i className="bi bi-pencil-square"></i>
                Richiedi Revisione (Admin)
              </button>
            )}

            {instance.status === 'InRevision' && (
              <button 
                onClick={handleFinalizeRevision}
                className="btn btn-primary w-100 py-3 fw-bold shadow-sm d-flex align-items-center justify-content-center gap-2"
                style={{ borderRadius: '12px' }}
              >
                <i className="bi bi-file-earmark-check-fill"></i>
                Finalizza Revisione (Admin)
              </button>
            )}
          </div>
        </div>

        {/* Campi Sezione Attiva */}
        <div className="col-12 col-md-8 col-lg-9">
          {structure.sections
            .filter(s => s.id === activeSectionId)
            .map((sec) => (
              <div key={sec.id} className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
                <h4 className="fw-bold text-dark mb-1">{sec.title}</h4>
                {sec.description && <p className="text-muted small border-bottom pb-3 mb-4">{sec.description}</p>}

                <div className="d-flex flex-column gap-3">
                  {sec.fields.map((field) => {
                    const value = answers[field.id] || '';
                    const hasBeenTouched = touchedFields.includes(field.id);
                    
                    return (
                      <div 
                        key={field.id} 
                        className={`card border-0 shadow-none p-3 checklist-item ${hasBeenTouched ? 'verified' : 'warning'}`}
                        style={{ borderLeftWidth: '5px' }}
                      >
                        <div className="row align-items-center g-3">
                          <div className="col-12 col-md-8">
                            <h6 className="fw-bold mb-1 text-dark">
                              {field.label}
                              {field.required && <span className="text-danger ms-1">*</span>}
                            </h6>
                            {field.description && <p className="text-muted small mb-0">{field.description}</p>}
                          </div>
                          
                          <div className="col-12 col-md-4 text-end">
                            {/* Input condizionali */}
                            {/* Input condizionali */}
                            {(field.type === 'boolean' || field.type === 'toggle') && (
                              <div className="form-check form-switch d-inline-block">
                                <input
                                  type="checkbox"
                                  className="form-check-input"
                                  checked={!!value}
                                  onChange={(e) => handleInputChange(field.id, e.target.checked)}
                                  disabled={isLocked || !canEdit}
                                  style={{ width: '50px', height: '24px', cursor: 'pointer' }}
                                />
                              </div>
                            )}

                            {field.type === 'checkbox' && (
                              <div className="form-check d-inline-block text-start">
                                <input
                                  type="checkbox"
                                  className="form-check-input"
                                  checked={!!value}
                                  onChange={(e) => handleInputChange(field.id, e.target.checked)}
                                  disabled={isLocked || !canEdit}
                                  style={{ width: '24px', height: '24px', cursor: 'pointer' }}
                                />
                              </div>
                            )}

                            {field.type === 'select' && (
                              <select
                                className="form-select form-select-sm"
                                value={value}
                                onChange={(e) => handleInputChange(field.id, e.target.value)}
                                disabled={isLocked || !canEdit}
                              >
                                <option value="">Seleziona...</option>
                                {field.options?.map(opt => {
                                  const optValue = typeof opt === 'string' ? opt : opt.value || '';
                                  const optLabel = typeof opt === 'string' ? opt : opt.label || '';
                                  return <option key={optValue} value={optValue}>{optLabel}</option>;
                                })}
                              </select>
                            )}

                            {field.type === 'text' && (
                              <input
                                type="text"
                                className="form-control form-control-sm"
                                value={value}
                                onChange={(e) => handleInputChange(field.id, e.target.value)}
                                placeholder={field.placeholder || "Inserisci testo..."}
                                disabled={isLocked || !canEdit}
                              />
                            )}

                            {field.type === 'textarea' && (
                              <textarea
                                className="form-control form-control-sm"
                                rows={3}
                                value={value}
                                onChange={(e) => handleInputChange(field.id, e.target.value)}
                                placeholder={field.placeholder || "Scrivi note o dettagli..."}
                                disabled={isLocked || !canEdit}
                              />
                            )}

                            {field.type === 'number' && (
                              <input
                                type="number"
                                className="form-control form-control-sm"
                                value={value}
                                onChange={(e) => handleInputChange(field.id, e.target.value)}
                                placeholder={field.placeholder || "0"}
                                disabled={isLocked || !canEdit}
                              />
                            )}

                            {field.type === 'date' && (
                              <input
                                type="date"
                                className="form-control form-control-sm"
                                value={value}
                                onChange={(e) => handleInputChange(field.id, e.target.value)}
                                disabled={isLocked || !canEdit}
                              />
                            )}

                            {field.type === 'file' && (
                              <div className="text-start">
                                <input
                                  type="file"
                                  className="form-control form-control-sm"
                                  onChange={(e) => {
                                    const filename = e.target.files?.[0]?.name || '';
                                    handleInputChange(field.id, filename);
                                  }}
                                  disabled={isLocked || !canEdit}
                                />
                                {value && <div className="small text-success mt-1">Caricato: {value}</div>}
                              </div>
                            )}

                            {field.type === 'signature' && (
                              value ? (
                                <div className="text-success small fw-semibold">
                                  <i className="bi bi-pen-fill me-1"></i> Firmato: {value}
                                </div>
                              ) : (
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-primary"
                                  onClick={() => handleInputChange(field.id, new Date().toLocaleString())}
                                  disabled={isLocked || !canEdit}
                                >
                                  Firma ora
                                </button>
                              )
                            )}

                            {field.type === 'link' && (
                              <a href={field.url} target="_blank" rel="noreferrer" className="btn btn-sm btn-outline-secondary">
                                <i className="bi bi-box-arrow-up-right me-1"></i> {field.button_text || 'Apri Collegamento'}
                              </a>
                            )}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
        </div>
      </div>

      {/* Dev Mode Debug Panel */}
      {document.cookie.includes('dev_mode=true') && (
        <div className="card border-danger shadow-sm mt-5 mb-4 bg-light">
          <div className="card-header bg-danger text-white d-flex align-items-center gap-2 py-2">
            <i className="bi bi-bug-fill"></i>
            <h6 className="mb-0 fw-bold">Pannello di Debug Sviluppatore (Dev Mode Attivo)</h6>
          </div>
          <div className="card-body py-3">
            <div className="row g-3">
              <div className="col-12 col-md-6">
                <h6 className="fw-semibold text-secondary mb-2">Risposte In Tempo Reale (State JSON)</h6>
                <pre className="bg-dark text-success p-3 rounded mb-0" style={{ fontSize: '0.75rem', maxHeight: '250px', overflowY: 'auto' }}>
                  {JSON.stringify(answers, null, 2)}
                </pre>
              </div>
              <div className="col-12 col-md-6">
                <h6 className="fw-semibold text-secondary mb-2">Log Attività SignalR (WebSocket)</h6>
                <div className="bg-dark text-info p-3 rounded font-monospace mb-0" style={{ fontSize: '0.75rem', maxHeight: '250px', overflowY: 'auto', minHeight: '150px' }}>
                  {socketLogs.length === 0 ? (
                    <span className="text-muted">Nessun evento WebSocket intercettato...</span>
                  ) : (
                    socketLogs.map((log, idx) => (
                      <div key={idx} className="mb-1">{log}</div>
                    ))
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
