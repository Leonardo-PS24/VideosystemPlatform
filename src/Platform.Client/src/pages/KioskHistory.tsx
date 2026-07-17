import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface HistoryItem {
  id: number;
  action: string;
  notes: string;
  status: string;
  createdBy: string;
  timestamp: string;
}

interface InstanceInfo {
  machineSerialNumber: string;
  template: {
    name: string;
  };
}

export default function KioskHistory() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [instance, setInstance] = useState<InstanceInfo | null>(null);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
        <div className="d-flex justify-content-between align-items-center">
          <div>
            <h6 className="text-uppercase text-muted small fw-bold mb-1">Storico Revisioni</h6>
            <h4 className="fw-bold mb-0 text-dark">
              {instance.template.name} <span className="text-secondary small fw-normal">Matricola: {instance.machineSerialNumber}</span>
            </h4>
          </div>
          <button onClick={() => navigate('/kiosk')} className="btn btn-outline-secondary">
            <i className="bi bi-arrow-left me-1"></i> Torna al Kiosk
          </button>
        </div>
      </div>

      {/* Timeline Storica */}
      <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
        <h5 className="fw-bold mb-4 text-dark">Timeline Attività</h5>

        {history.length === 0 ? (
          <div className="text-center text-muted py-4">Nessuna attività registrata per questa macchina.</div>
        ) : (
          <div className="position-relative ps-4" style={{ borderLeft: '3px solid #e9ecef' }}>
            {history.map((item, index) => (
              <div key={item.id} className="mb-4 position-relative">
                {/* Indicatore grafico */}
                <div 
                  className="position-absolute bg-white rounded-circle d-flex align-items-center justify-content-center border"
                  style={{ 
                    left: '-34px', 
                    top: '0px', 
                    width: '24px', 
                    height: '24px',
                    borderColor: item.action === 'Created' ? '#198754' : '#2563eb'
                  }}
                >
                  <div 
                    className="rounded-circle"
                    style={{ 
                      width: '12px', 
                      height: '12px',
                      backgroundColor: item.action === 'Created' ? '#198754' : '#2563eb'
                    }}
                  ></div>
                </div>

                {/* Contenuto log */}
                <div className="ms-2">
                  <div className="d-flex justify-content-between align-items-start flex-wrap gap-2">
                    <h6 className="fw-bold text-dark mb-1">
                      {item.action === 'Created' && 'Creazione Checklist'}
                      {item.action === 'Completed' && 'Checklist Completata'}
                      {item.action === 'RevisionStarted' && 'Revisione Avviata'}
                      {item.action === 'RevisionFinalized' && 'Revisione Approvata'}
                      {item.action !== 'Created' && item.action !== 'Completed' && item.action !== 'RevisionStarted' && item.action !== 'RevisionFinalized' && item.action}
                    </h6>
                    <span className="small text-muted">{new Date(item.timestamp).toLocaleString()}</span>
                  </div>

                  <p className="text-muted small mb-2">{item.notes}</p>
                  
                  <div className="d-flex align-items-center gap-3 text-secondary small">
                    <span>
                      <i className="bi bi-person-fill me-1"></i>
                      Operatore: <strong>{item.createdBy}</strong>
                    </span>
                    <span>
                      <i className="bi bi-tag-fill me-1"></i>
                      Stato: <strong>{item.status}</strong>
                    </span>
                  </div>
                </div>
                
                {index < history.length - 1 && <hr className="my-3 text-black-50" />}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
