import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { HubConnectionBuilder } from '@microsoft/signalr';
import KioskTemplates from './KioskTemplates';

interface TemplateInfo {
  id: number;
  name: string;
  version: string;
  description: string;
}

interface InstanceInfo {
  id: number;
  machineSerial: string;
  status: 'Draft' | 'InProgress' | 'Completed' | 'InRevision' | 'UnderRevision' | 'PendingApproval' | 'Finalized';
  progress: number;
  createdAt: string;
  completedAt: string | null;
  template: {
    name: string;
  };
}

interface UserData {
  username: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface KioskDashboardProps {
  user: UserData | null;
}

export default function KioskDashboard({ user }: KioskDashboardProps) {
  const [data, setData] = useState<{
    canCreate: boolean;
    canDelete: boolean;
    recentInstances: InstanceInfo[];
    availableTemplates: TemplateInfo[];
  } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Form states
  const [selectedTemplate, setSelectedTemplate] = useState<number | ''>('');
  const [serialNumber, setSerialNumber] = useState('');
  const [creating, setCreating] = useState(false);

  const navigate = useNavigate();

  const loadData = () => {
    fetch('/api/Kiosk')
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento dei dati.');
        return res.json();
      })
      .then((json) => {
        setData(json);
        if (json.availableTemplates.length > 0) {
          setSelectedTemplate(json.availableTemplates[0].id);
        }
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setError('Impossibile caricare i dati del Kiosk.');
        setLoading(false);
      });
  };

  useEffect(() => {
    loadData();

    // Setup SignalR per aggiornamenti in tempo reale
    const connection = new HubConnectionBuilder()
      .withUrl('/kioskhub')
      .withAutomaticReconnect()
      .build();

    connection.start()
      .catch(err => console.error("SignalR Connection Error (Dashboard):", err));

    connection.on('NewInstanceCreated', () => loadData());
    connection.on('UpdateProgress', () => loadData());
    connection.on('InstanceCompleted', () => loadData());
    connection.on('UpdateStatus', () => loadData());

    return () => {
      if (connection) {
        connection.off('NewInstanceCreated');
        connection.off('UpdateProgress');
        connection.off('InstanceCompleted');
        connection.off('UpdateStatus');
        connection.stop().catch(() => {});
      }
    };
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTemplate || !serialNumber.trim()) return;

    setCreating(true);
    try {
      const response = await fetch('/api/Kiosk', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          templateId: Number(selectedTemplate),
          machineSerial: serialNumber.trim(),
        }),
      });

      if (response.ok) {
        const instance = await response.json();
        navigate(`/kiosk/compile/${instance.id}`);
      } else {
        alert('Errore nella creazione della checklist.');
      }
    } catch (err) {
      console.error(err);
      alert('Errore di connessione.');
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Sei sicuro di voler eliminare questa compilazione?')) return;

    try {
      const response = await fetch(`/api/Kiosk/${id}`, {
        method: 'DELETE',
      });

      if (response.ok) {
        loadData();
      } else {
        alert("Impossibile eliminare l'istanza.");
      }
    } catch (err) {
      console.error(err);
    }
  };

  const getStatusBadge = (status: InstanceInfo['status']) => {
    switch (status) {
      case 'Draft':
        return <span className="badge bg-warning text-dark">Bozza</span>;
      case 'InProgress':
        return <span className="badge bg-warning text-dark">In Corso</span>;
      case 'Completed':
        return <span className="badge bg-success">Completato</span>;
      case 'InRevision':
      case 'UnderRevision':
        return <span className="badge bg-info text-dark">In Revisione</span>;
      case 'PendingApproval':
        return <span className="badge bg-danger">Attesa Approvazione</span>;
      case 'Finalized':
        return <span className="badge bg-primary">Finalizzato</span>;
      default:
        return <span className="badge bg-secondary">{status}</span>;
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

  if (error || !data) {
    return (
      <div className="alert alert-danger" role="alert">
        <i className="bi bi-exclamation-triangle-fill me-2"></i>
        {error}
      </div>
    );
  }

  return (
    <div className="d-flex flex-column gap-4">
      {user?.roles.includes('Admin') && (
        <KioskTemplates />
      )}

      <div className="row g-4">
        {/* Colonna di sinistra: Lista recenti */}
        <div className="col-12 col-lg-8">
          <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
            <h5 className="fw-bold mb-4 text-dark">Compilazioni Recenti</h5>
            
            <div className="table-responsive">
              <table className="table table-hover align-middle">
                <thead>
                  <tr className="text-secondary small">
                    <th>Machine Serial</th>
                    <th>Template</th>
                    <th>Progresso</th>
                    <th>Stato</th>
                    <th>Creazione</th>
                    <th className="text-end">Azioni</th>
                  </tr>
                </thead>
                <tbody>
                  {data.recentInstances.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="text-center text-muted py-4">
                        Nessuna compilazione recente trovata.
                      </td>
                    </tr>
                  ) : (
                    data.recentInstances.map((ins) => {
                      const isCompleted = ins.status === 'Completed' || ins.status === 'Finalized';
                      const serial = ins.machineSerial || (ins as any).machineSerialNumber || (ins as any).MachineSerialNumber || (ins as any).MachineSerial || 'N/D';
                      
                      return (
                        <tr 
                          key={ins.id}
                          onClick={() => navigate(`/kiosk/compile/${ins.id}`)}
                          style={{ 
                            cursor: 'pointer',
                            backgroundColor: isCompleted ? '#eceff1' : 'transparent',
                            opacity: isCompleted ? 0.7 : 1
                          }}
                          className={isCompleted ? 'text-muted' : ''}
                        >
                          <td className="fw-bold text-dark">{serial}</td>
                          <td className="small text-muted">{ins.template.name}</td>
                          <td style={{ width: '150px' }}>
                            <div className="d-flex align-items-center gap-2">
                              <div className="progress w-100" style={{ height: '6px' }}>
                                <div 
                                  className={`progress-bar ${isCompleted ? 'bg-success' : 'bg-warning'}`}
                                  style={{ width: `${ins.progress}%` }}
                                ></div>
                              </div>
                              <span className="small fw-semibold">{ins.progress}%</span>
                            </div>
                          </td>
                          <td>{getStatusBadge(ins.status)}</td>
                          <td className="small text-muted">{new Date(ins.createdAt).toLocaleString()}</td>
                          <td className="text-end">
                            <div className="btn-group btn-group-sm" onClick={(e) => e.stopPropagation()}>
                              <button 
                                onClick={() => navigate(`/kiosk/compile/${ins.id}`)}
                                className="btn btn-outline-primary"
                                title="Visualizza/Compila"
                              >
                                <i className="bi bi-pencil-square"></i>
                              </button>
                              <button 
                                onClick={() => navigate(`/kiosk/history/${ins.id}`)}
                                className="btn btn-outline-info"
                                title="Storico Revisioni"
                              >
                                <i className="bi bi-clock-history"></i>
                              </button>
                              {data.canDelete && (
                                <button 
                                  onClick={() => handleDelete(ins.id)}
                                  className="btn btn-outline-danger"
                                  title="Elimina"
                                >
                                  <i className="bi bi-trash"></i>
                                </button>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* Colonna di destra: Avvia nuova o messaggio */}
        {data.canCreate && (
          <div className="col-12 col-lg-4">
            <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
              <h5 className="fw-bold mb-4 text-dark">Nuova Compilazione</h5>
              
              <form onSubmit={handleCreate}>
                <div className="mb-3">
                  <label className="form-label small fw-semibold text-muted">Seleziona Modello (Template)</label>
                  <select 
                    className="form-select"
                    value={selectedTemplate}
                    onChange={(e) => setSelectedTemplate(Number(e.target.value))}
                    required
                  >
                    {data.availableTemplates.map(t => (
                      <option key={t.id} value={t.id}>{t.name} (v{t.version})</option>
                    ))}
                  </select>
                </div>

                <div className="mb-4">
                  <label className="form-label small fw-semibold text-muted">Matricola Macchina (Serial)</label>
                  <input 
                    type="text"
                    className="form-control"
                    placeholder="es. KSK-2026-99"
                    value={serialNumber}
                    onChange={(e) => setSerialNumber(e.target.value)}
                    required
                  />
                </div>

                <button 
                  type="submit" 
                  className="btn btn-primary w-100 py-2 fw-semibold"
                  disabled={creating}
                >
                  {creating ? (
                    <span className="spinner-border spinner-border-sm me-2"></span>
                  ) : null}
                  Avvia Checklist
                </button>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
