import { useEffect, useState } from 'react';

interface UserItem {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export default function AdminUsers() {
  const [users, setUsers] = useState<UserItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [availableRoles, setAvailableRoles] = useState<string[]>(['User', 'Admin', 'Developer']);

  // Modal / Form states
  const [showModal, setShowModal] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  
  // Field states
  const [userId, setUserId] = useState('');
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [role, setRole] = useState('User');
  const [isActive, setIsActive] = useState(true);
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const fetchUsers = () => {
    fetch('/api/Admin/Users')
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento degli utenti.');
        return res.json();
      })
      .then((data) => {
        setUsers(data);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setError('Impossibile caricare la lista utenti.');
        setLoading(false);
      });
  };

  const fetchRoles = () => {
    fetch('/api/Permissions/Roles')
      .then((res) => {
        if (res.ok) return res.json();
        throw new Error();
      })
      .then((data) => {
        if (data.roles) {
          setAvailableRoles(data.roles.map((r: any) => r.roleName));
        }
      })
      .catch((err) => {
        console.error("Errore caricamento ruoli:", err);
      });
  };

  useEffect(() => {
    fetchUsers();
    fetchRoles();
  }, []);

  const handleOpenCreate = () => {
    setModalMode('create');
    setUserId('');
    setUsername('');
    setEmail('');
    setFullName('');
    setRole('User');
    setIsActive(true);
    setPassword('');
    setConfirmPassword('');
    setFormError(null);
    setFormSuccess(null);
    setShowModal(true);
  };

  const handleOpenEdit = (user: UserItem) => {
    setModalMode('edit');
    setUserId(user.id);
    setUsername(user.username);
    setEmail(user.email);
    setFullName(user.fullName || '');
    setRole(user.role);
    setIsActive(user.isActive);
    setPassword('');
    setConfirmPassword('');
    setFormError(null);
    setFormSuccess(null);
    setShowModal(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(null);

    // Password validation for create/edit
    if (password && password !== confirmPassword) {
      setFormError('Le password non coincidono.');
      return;
    }

    setSubmitting(true);
    try {
      const url = modalMode === 'create' ? '/api/Admin/Users' : `/api/Admin/Users/${userId}`;
      const method = modalMode === 'create' ? 'POST' : 'PUT';

      const response = await fetch(url, {
        method: method,
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          id: userId || undefined,
          username,
          email,
          fullName,
          role,
          isActive,
          password: password || undefined,
          confirmPassword: confirmPassword || undefined,
        }),
      });

      const resData = await response.json();

      if (response.ok) {
        setFormSuccess(resData.message || 'Operazione completata con successo!');
        fetchUsers();
        setTimeout(() => setShowModal(false), 1500);
      } else {
        setFormError(resData.message || 'Errore durante il salvataggio dei dati.');
      }
    } catch (err) {
      console.error(err);
      setFormError('Errore di connessione con il server.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleStatus = async (id: string) => {
    try {
      const response = await fetch(`/api/Admin/Users/${id}/toggle-status`, {
        method: 'POST',
      });
      if (response.ok) {
        fetchUsers();
      } else {
        const data = await response.json();
        alert(data.message || 'Impossibile cambiare lo stato.');
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleDelete = async (id: string, username: string) => {
    if (!window.confirm(`Sei sicuro di voler eliminare definitivamente l'utente ${username}?`)) return;

    try {
      const response = await fetch(`/api/Admin/Users/${id}`, {
        method: 'DELETE',
      });

      const data = await response.json();
      if (response.ok) {
        alert(data.message || 'Utente eliminato.');
        fetchUsers();
      } else {
        alert(data.message || "Impossibile eliminare l'utente.");
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
          <h5 className="fw-bold mb-0 text-dark">Gestione Utenti Piattaforma</h5>
          <button onClick={handleOpenCreate} className="btn btn-primary d-flex align-items-center gap-1">
            <i className="bi bi-person-plus-fill"></i> Nuovo Utente
          </button>
        </div>

        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead>
              <tr className="text-secondary small">
                <th>Nome Completo</th>
                <th>Username</th>
                <th>Email</th>
                <th>Ruolo</th>
                <th>Stato</th>
                <th>Data Creazione</th>
                <th className="text-end">Azioni</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>
                    <div className="fw-bold text-dark">{u.fullName || 'N/A'}</div>
                  </td>
                  <td className="small text-muted">{u.username}</td>
                  <td className="small text-muted">{u.email}</td>
                  <td>
                    <span className={`badge ${u.role === 'Admin' ? 'bg-danger' : 'bg-secondary'}`}>
                      {u.role}
                    </span>
                  </td>
                  <td>
                    <div className="form-check form-switch">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        checked={u.isActive}
                        onChange={() => handleToggleStatus(u.id)}
                        style={{ cursor: 'pointer' }}
                      />
                      <span className={`small ms-1 ${u.isActive ? 'text-success' : 'text-danger'}`}>
                        {u.isActive ? 'Attivo' : 'Disattivato'}
                      </span>
                    </div>
                  </td>
                  <td className="small text-muted">{new Date(u.createdAt).toLocaleDateString()}</td>
                  <td className="text-end">
                    <div className="btn-group btn-group-sm">
                      <button 
                        onClick={() => handleOpenEdit(u)}
                        className="btn btn-outline-primary"
                        title="Modifica"
                      >
                        <i className="bi bi-pencil"></i>
                      </button>
                      <button 
                        onClick={() => handleDelete(u.id, u.username)}
                        className="btn btn-outline-danger"
                        title="Elimina"
                      >
                        <i className="bi bi-trash"></i>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Creazione/Modifica */}
      {showModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">
                  {modalMode === 'create' ? 'Crea Nuovo Utente' : 'Modifica Utente'}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>
              
              <form onSubmit={handleSubmit}>
                <div className="modal-body py-3">
                  {formError && (
                    <div className="alert alert-danger py-2 small mb-3">{formError}</div>
                  )}
                  {formSuccess && (
                    <div className="alert alert-success py-2 small mb-3">{formSuccess}</div>
                  )}

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-muted">Nome Completo</label>
                    <input
                      type="text"
                      className="form-control"
                      value={fullName}
                      onChange={(e) => setFullName(e.target.value)}
                      placeholder="Mario Rossi"
                      required
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-muted">Username</label>
                    <input
                      type="text"
                      className="form-control"
                      value={username}
                      onChange={(e) => setUsername(e.target.value)}
                      placeholder="mario.rossi"
                      required
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-muted">Email Aziendale</label>
                    <input
                      type="email"
                      className="form-control"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="mario.rossi@videosystem.it"
                      required
                    />
                  </div>

                  <div className="row g-3 mb-3">
                    <div className="col-6">
                      <label className="form-label small fw-semibold text-muted">Ruolo</label>
                      <select
                        className="form-select"
                        value={role}
                        onChange={(e) => setRole(e.target.value)}
                      >
                        {availableRoles.map((r) => (
                          <option key={r} value={r}>{r}</option>
                        ))}
                      </select>
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

                  {/* Campi password per resettarla o crearla */}
                  <div className="border-top pt-3 mt-3">
                    <h6 className="fw-bold small text-secondary mb-3">
                      {modalMode === 'create' ? 'Gestione Password (Opzionale)' : 'Resetta Password (Lascia vuoto se non vuoi modificarla)'}
                    </h6>
                    
                    <div className="mb-3">
                      <label className="form-label small fw-semibold text-muted">Nuova Password</label>
                      <input
                        type="password"
                        className="form-control"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        placeholder="••••••••"
                      />
                    </div>
                    
                    <div className="mb-2">
                      <label className="form-label small fw-semibold text-muted">Conferma Password</label>
                      <input
                        type="password"
                        className="form-control"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        placeholder="••••••••"
                      />
                    </div>
                  </div>
                </div>

                <div className="modal-footer border-0 pt-0">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Annulla
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={submitting}>
                    {submitting ? <span className="spinner-border spinner-border-sm me-1"></span> : null}
                    Salva
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
