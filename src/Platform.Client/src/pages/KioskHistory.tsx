import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';

interface HistoryItem {
  id: number;
  action: string;
  notes: string;
  status: string;
  userId: string;
  timestamp: string;
  dataJson: string;
}

interface InstanceInfo {
  machineSerialNumber: string;
  template: {
    name: string;
    structureJson: string;
  };
}

export default function KioskHistory() {
  const { id } = useParams<{ id: string }>();

  const [instance, setInstance] = useState<InstanceInfo | null>(null);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [fieldLabels, setFieldLabels] = useState<Record<string, string>>({});
  const [expandedItems, setExpandedItems] = useState<Record<number, boolean>>({});

  useEffect(() => {
    if (instance?.template?.structureJson) {
      try {
        const struct = JSON.parse(instance.template.structureJson);
        const labels: Record<string, string> = {};
        struct.sections?.forEach((s: any) => {
          s.fields?.forEach((f: any) => {
            labels[f.id] = f.label;
          });
        });
        setFieldLabels(labels);
      } catch (err) {
        console.error("Errore nel parsing della struttura del template:", err);
      }
    }
  }, [instance]);

  const toggleExpand = (itemId: number) => {
    setExpandedItems(prev => ({ ...prev, [itemId]: !prev[itemId] }));
  };

  const computeDiff = (itemJson: string, prevJson: string) => {
    try {
      const current = JSON.parse(itemJson || '{}');
      const prev = JSON.parse(prevJson || '{}');
      
      const diffs: { label: string; oldValue: string; newValue: string }[] = [];
      
      const allKeys = Array.from(new Set([
        ...Object.keys(current),
        ...Object.keys(prev)
      ])).filter(k => !k.startsWith('_'));
      
      allKeys.forEach(key => {
        const val1 = prev[key] !== undefined ? String(prev[key]) : '';
        const val2 = current[key] !== undefined ? String(current[key]) : '';
        
        if (val1 !== val2) {
          diffs.push({
            label: fieldLabels[key] || key,
            oldValue: val1,
            newValue: val2
          });
        }
      });
      
      return diffs;
    } catch (err) {
      console.error(err);
      return [];
    }
  };

  useEffect(() => {
    fetch(`/api/Kiosk/${id}/history`)
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento della cronologia.');
        return res.json();
      })
      .then((data) => {
        setInstance(data.instance);
        setHistory(data.history);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setError('Impossibile caricare lo storico delle revisioni.');
        setLoading(false);
      });
  }, [id]);

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '300px' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Caricamento...</span>
        </div>
      </div>
    );
  }

  if (error || !instance) {
    return (
      <div className="alert alert-danger" role="alert">
        <i className="bi bi-exclamation-triangle-fill me-2"></i>
        {error}
      </div>
    );
  }

  return (
    <div>
      {/* Intestazione */}
      <div className="card border-0 shadow-sm p-4 mb-4 bg-white" style={{ borderRadius: '12px' }}>
        <div>
          <h6 className="text-uppercase text-muted small fw-bold mb-1">Storico Revisioni</h6>
          <h4 className="fw-bold mb-0 text-dark">
            {instance.template.name} <span className="text-secondary small fw-normal">Matricola: {instance.machineSerialNumber}</span>
          </h4>
        </div>
      </div>

      {/* Timeline Storica */}
      <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
        <h5 className="fw-bold mb-4 text-dark">Timeline Attività</h5>

        {history.length === 0 ? (
          <div className="text-center text-muted py-4">Nessuna attività registrata per questa macchina.</div>
        ) : (
          <div className="position-relative ps-4">
            {history.map((item, index) => {
              const isExpanded = !!expandedItems[item.id];
              const prevJson = (index + 1 < history.length) ? history[index + 1].dataJson : "{}";
              const diffs = computeDiff(item.dataJson, prevJson);
              
              return (
                <div key={item.id} className="mb-4 position-relative">
                  {/* Linea connettrice verticale (non per l'ultimo elemento) */}
                  {index < history.length - 1 && (
                    <div 
                      className="position-absolute"
                      style={{
                        left: '-23px',
                        top: '24px',
                        bottom: '-24px',
                        width: '2px',
                        backgroundColor: '#e9ecef',
                        zIndex: 1
                      }}
                    ></div>
                  )}

                  {/* Indicatore grafico (Pallino) */}
                  <div 
                    className="position-absolute bg-white rounded-circle d-flex align-items-center justify-content-center border"
                    style={{ 
                      left: '-34px', 
                      top: '0px', 
                      width: '24px', 
                      height: '24px',
                      borderColor: item.action === 'Created' ? '#198754' : 
                                   item.action === 'RevisionRejected' ? '#dc3545' : 
                                   item.action === 'RevisionFinalized' ? '#0d6efd' : '#2563eb',
                      zIndex: 2
                    }}
                  >
                    <div 
                      className="rounded-circle"
                      style={{ 
                        width: '10px', 
                        height: '10px',
                        backgroundColor: item.action === 'Created' ? '#198754' : 
                                         item.action === 'RevisionRejected' ? '#dc3545' : 
                                         item.action === 'RevisionFinalized' ? '#0d6efd' : '#2563eb'
                      }}
                    ></div>
                  </div>

                  {/* Contenuto log */}
                  <div 
                    className="ms-2 card border shadow-none p-3" 
                    style={{ 
                      borderRadius: '12px', 
                      cursor: 'pointer',
                      backgroundColor: isExpanded ? '#fcfdfd' : '#ffffff',
                      borderLeft: isExpanded ? '4px solid #0d6efd' : '1px solid #eef2f6',
                      transition: 'all 0.2s ease-in-out'
                    }}
                    onClick={() => toggleExpand(item.id)}
                  >
                    <div className="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-2">
                      <div className="d-flex align-items-center gap-2">
                        <h6 className="fw-bold text-dark mb-0">
                          {item.action === 'Created' && 'Creazione Checklist'}
                          {item.action === 'Completed' && 'Prima Compilazione'}
                          {item.action === 'RevisionStarted' && 'Revisione Avviata'}
                          {item.action === 'RevisionFinalized' && 'Revisione Approvata (Admin)'}
                          {item.action === 'RevisionRejected' && 'Revisione Rifiutata (Admin)'}
                          {item.action === 'ProposedRevision' && 'Revisione Inviata per Approvazione'}
                          {item.action !== 'Created' && item.action !== 'Completed' && item.action !== 'RevisionStarted' && item.action !== 'RevisionFinalized' && item.action !== 'RevisionRejected' && item.action !== 'ProposedRevision' && item.action}
                        </h6>
                        <span className="badge bg-secondary-subtle text-secondary small px-2 py-0.5 rounded-pill" style={{ fontSize: '0.7rem' }}>
                          Stato: {item.status}
                        </span>
                      </div>
                      <div className="d-flex align-items-center gap-2 text-muted small">
                        <span>{new Date(item.timestamp).toLocaleString()}</span>
                        <i className={`bi ${isExpanded ? 'bi-chevron-up text-primary' : 'bi-chevron-down text-muted'}`}></i>
                      </div>
                    </div>

                    <p className="text-muted small mb-2">{item.notes}</p>
                    
                    <div className="d-flex align-items-center gap-3 text-secondary small">
                      <span>
                        <i className="bi bi-person-fill me-1"></i>
                        Operatore: <strong>{item.userId}</strong>
                      </span>
                    </div>

                    {/* Diff espandibile */}
                    {isExpanded && (
                      <div className="mt-3 pt-3 border-top border-light">
                        <div className="d-flex align-items-center gap-2 mb-2 text-primary">
                          <i className="bi bi-info-circle-fill small"></i>
                          <span className="fw-bold small text-uppercase tracking-wide" style={{ fontSize: '0.75rem' }}>Dettagli Variazioni Dati</span>
                        </div>
                        {diffs.length === 0 ? (
                          <div className="text-muted small py-1 bg-light rounded text-center">Nessuna variazione dei dati registrata per questo passaggio.</div>
                        ) : (
                          <div className="d-flex flex-column gap-2 mt-2">
                            {diffs.map((d, dIdx) => {
                              const formatVal = (val: string) => {
                                if (val === 'True') return 'Sì';
                                if (val === 'False') return 'No';
                                return val || 'Non compilato / Vuoto';
                              };
                              return (
                                <div key={dIdx} className="d-flex flex-wrap align-items-center justify-content-between p-2 rounded bg-light border border-light-subtle" style={{ fontSize: '0.85rem' }}>
                                  <span className="fw-semibold text-dark me-2">{d.label}</span>
                                  <div className="d-flex align-items-center gap-2">
                                    <span className="badge text-danger-emphasis bg-danger-subtle border border-danger-subtle px-2 py-1 rounded font-monospace" style={{ fontSize: '0.75rem' }}>
                                      {formatVal(d.oldValue)}
                                    </span>
                                    <i className="bi bi-arrow-right text-muted"></i>
                                    <span className="badge text-success-emphasis bg-success-subtle border border-success-subtle px-2 py-1 rounded font-monospace" style={{ fontSize: '0.75rem' }}>
                                      {formatVal(d.newValue)}
                                    </span>
                                  </div>
                                </div>
                              );
                            })}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
