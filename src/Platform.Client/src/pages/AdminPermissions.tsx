import React, { useEffect, useState } from 'react';

interface PermissionCatalogItem {
  id: number;
  key: string;
  area: string;
  module: string;
  action: string;
  description: string;
}

interface PolicyItem {
  id: number;
  name: string;
  description?: string;
  isSystemPolicy: boolean;
  permissions: PermissionCatalogItem[];
}

interface UserAssignmentItem {
  id: number;
  userId: string;
  policyId: number;
  policyName: string;
  companyScope: string;
  departmentScope: string;
}

interface UserMatrixItem {
  id: string;
  username: string;
  fullName: string;
  email: string;
  role: string;
}

const DEPARTMENTS = ['Tutti', 'Magazzino', 'Commerciale', 'Assistenza', 'Amministrazione', 'IT'];
const COMPANIES = ['Tutte', 'Pharmaself24', 'Skriptkiosk'];

export default function AdminPermissions() {
  const [activeTab, setActiveTab] = useState<'policies' | 'users' | 'roles'>('policies');

  // State Policy & Catalog
  const [catalog, setCatalog] = useState<PermissionCatalogItem[]>([]);
  const [policies, setPolicies] = useState<PolicyItem[]>([]);
  const [loadingPolicies, setLoadingPolicies] = useState(true);

  // State Users & Assignments
  const [users, setUsers] = useState<UserMatrixItem[]>([]);
  const [userAssignments, setUserAssignments] = useState<Record<string, UserAssignmentItem[]>>({});
  const [loadingUsers, setLoadingUsers] = useState(false);

  // Modal Policy State
  const [showPolicyModal, setShowPolicyModal] = useState(false);
  const [editingPolicyId, setEditingPolicyId] = useState<number>(0);
  const [policyName, setPolicyName] = useState('');
  const [policyDescription, setPolicyDescription] = useState('');
  const [selectedPermIds, setSelectedPermIds] = useState<number[]>([]);
  const [savingPolicy, setSavingPolicy] = useState(false);

  // Modal User Assignment State
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selectedUserName, setSelectedUserName] = useState('');
  const [selectedPolicyId, setSelectedPolicyId] = useState<number>(0);
  const [selectedCompanyScope, setSelectedCompanyScope] = useState('ALL');
  const [selectedDepartmentScope, setSelectedDepartmentScope] = useState('ALL');
  const [savingAssign, setSavingAssign] = useState(false);

  const [feedback, setFeedback] = useState<{ type: 'success' | 'danger'; message: string } | null>(null);

  const loadPbacData = async () => {
    setLoadingPolicies(true);
    try {
      const ts = new Date().getTime();
      const catRes = await fetch(`/api/PbacAdmin/Catalog?t=${ts}`);
      if (catRes.ok) {
        try {
          const catData = await catRes.json();
          if (Array.isArray(catData)) setCatalog(catData);
        } catch (e) {
          console.error("Errore parse catalogo", e);
        }
      }

      const polRes = await fetch(`/api/PbacAdmin/Policies?t=${ts}`);
      if (polRes.ok) {
        try {
          const polData = await polRes.json();
          if (Array.isArray(polData)) setPolicies(polData);
        } catch (e) {
          console.error("Errore parse policies", e);
        }
      }
    } catch (err) {
      console.error('Errore nel caricamento dei dati PBAC:', err);
    } finally {
      setLoadingPolicies(false);
    }
  };

  const loadUsersData = async () => {
    setLoadingUsers(true);
    try {
      const ts = new Date().getTime();
      const res = await fetch(`/api/Admin/Users?t=${ts}`);
      if (res.ok) {
        const userData: UserMatrixItem[] = await res.json();
        setUsers(userData);

        // Carica le assegnazioni policy per ciascun utente
        const assignmentsMap: Record<string, UserAssignmentItem[]> = {};
        await Promise.all(
          userData.map(async (u) => {
            const aRes = await fetch(`/api/PbacAdmin/UserAssignments/${u.id}?t=${ts}`);
            if (aRes.ok) {
              assignmentsMap[u.id] = await aRes.json();
            }
          })
        );
        setUserAssignments(assignmentsMap);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoadingUsers(false);
    }
  };

  useEffect(() => {
    loadPbacData();
  }, []);

  const handleOpenNewPolicy = () => {
    if (catalog.length === 0) {
      loadPbacData();
    }
    setEditingPolicyId(0);
    setPolicyName('');
    setPolicyDescription('');
    setSelectedPermIds([]);
    setShowPolicyModal(true);
  };

  const handleOpenEditPolicy = (p: PolicyItem) => {
    setEditingPolicyId(p.id);
    setPolicyName(p.name);
    setPolicyDescription(p.description || '');
    setSelectedPermIds(p.permissions.map((perm) => perm.id));
    setShowPolicyModal(true);
  };

  const handleTogglePerm = (permId: number) => {
    setSelectedPermIds((prev) =>
      prev.includes(permId) ? prev.filter((id) => id !== permId) : [...prev, permId]
    );
  };

  const handleSavePolicy = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!policyName.trim()) return;

    setSavingPolicy(true);
    try {
      const res = await fetch('/api/PbacAdmin/Policies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          id: editingPolicyId,
          name: policyName.trim(),
          description: policyDescription.trim(),
          permissionCatalogIds: selectedPermIds
        })
      });

      if (res.ok) {
        setFeedback({ type: 'success', message: 'Policy salvata con successo!' });
        setShowPolicyModal(false);
        loadPbacData();
      } else {
        const err = await res.json();
        setFeedback({ type: 'danger', message: err.message || 'Errore nel salvataggio.' });
      }
    } catch (err) {
      console.error(err);
      setFeedback({ type: 'danger', message: 'Errore di connessione.' });
    } finally {
      setSavingPolicy(false);
    }
  };

  const handleDeletePolicy = async (id: number, name: string) => {
    if (!window.confirm(`Sei sicuro di voler eliminare la Policy '${name}'?`)) return;

    try {
      const res = await fetch(`/api/PbacAdmin/Policies/${id}`, { method: 'DELETE' });
      if (res.ok) {
        setFeedback({ type: 'success', message: 'Policy eliminata con successo.' });
        loadPbacData();
      } else {
        const err = await res.json();
        setFeedback({ type: 'danger', message: err.message || 'Impossibile eliminare la policy.' });
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleOpenAssignModal = (u: UserMatrixItem) => {
    setSelectedUserId(u.id);
    setSelectedUserName(u.fullName || u.username);
    setSelectedPolicyId(policies[0]?.id || 0);
    setSelectedCompanyScope('ALL');
    setSelectedDepartmentScope('ALL');
    setShowAssignModal(true);
  };

  const handleSaveUserAssignment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedUserId || selectedPolicyId <= 0) return;

    setSavingAssign(true);
    try {
      const res = await fetch('/api/PbacAdmin/UserAssignments', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: selectedUserId,
          policyId: selectedPolicyId,
          companyScope: selectedCompanyScope,
          departmentScope: selectedDepartmentScope
        })
      });

      if (res.ok) {
        setFeedback({ type: 'success', message: 'Policy assegnata all\'utente con successo!' });
        setShowAssignModal(false);
        loadUsersData();
      } else {
        setFeedback({ type: 'danger', message: 'Errore nell\'assegnazione della policy.' });
      }
    } catch (err) {
      console.error(err);
    } finally {
      setSavingAssign(false);
    }
  };

  const handleRemoveUserAssignment = async (assignmentId: number) => {
    if (!window.confirm('Rimuovere questa assegnazione di policy dall\'utente?')) return;

    try {
      const res = await fetch(`/api/PbacAdmin/UserAssignments/${assignmentId}`, { method: 'DELETE' });
      if (res.ok) {
        loadUsersData();
      }
    } catch (err) {
      console.error(err);
    }
  };

  // Raggruppa i permessi del catalogo per Area
  const catalogByArea = catalog.reduce((acc, perm) => {
    acc[perm.area] = acc[perm.area] || [];
    acc[perm.area].push(perm);
    return acc;
  }, {} as Record<string, PermissionCatalogItem[]>);

  return (
    <div>
      <div className="card border-0 shadow-sm p-4 bg-white" style={{ borderRadius: '12px' }}>
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h5 className="fw-bold mb-1 text-dark">Gestione Permessi PBAC & Policy Aziendali</h5>
            <small className="text-muted">Profilazione accessi basata su Policy e filtri di ambito Sottoazienda e Reparto</small>
          </div>
        </div>

        {feedback && (
          <div className={`alert alert-${feedback.type} alert-dismissible fade show mb-4`} role="alert">
            {feedback.message}
            <button type="button" className="btn-close" onClick={() => setFeedback(null)}></button>
          </div>
        )}

        {/* Tab Selection */}
        <ul className="nav nav-tabs mb-4">
          <li className="nav-item">
            <button
              className={`nav-link fw-semibold d-flex align-items-center gap-2 ${activeTab === 'policies' ? 'active text-primary' : 'text-secondary'}`}
              onClick={() => setActiveTab('policies')}
            >
              <i className="bi bi-shield-check"></i> Pacchetti Policy Aziendali ({policies.length})
            </button>
          </li>
          <li className="nav-item">
            <button
              className={`nav-link fw-semibold d-flex align-items-center gap-2 ${activeTab === 'users' ? 'active text-primary' : 'text-secondary'}`}
              onClick={() => {
                setActiveTab('users');
                loadUsersData();
              }}
            >
              <i className="bi bi-people-fill"></i> Assegnazioni Utenti & Scope
            </button>
          </li>
        </ul>

        {/* TAB 1: PACCHETTI POLICY */}
        {activeTab === 'policies' && (
          <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="text-muted small">Crea e gestisci pacchetti di permessi riutilizzabili per tutta l'azienda.</span>
              <button className="btn btn-sm btn-primary d-flex align-items-center gap-1" onClick={handleOpenNewPolicy}>
                <i className="bi bi-plus-circle-fill"></i> Crea Nuova Policy
              </button>
            </div>

            {loadingPolicies ? (
              <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status"></div>
                <div className="text-muted small mt-2">Caricamento policy...</div>
              </div>
            ) : (
              <div className="row g-3">
                {policies.map((p) => (
                  <div key={p.id} className="col-md-6 col-lg-4">
                    <div className="card h-100 border shadow-sm rounded-3">
                      <div className="card-header bg-light d-flex align-items-center justify-content-between py-3">
                        <div className="fw-bold text-dark d-flex align-items-center gap-2">
                          <i className={`bi ${p.isSystemPolicy ? 'bi-shield-lock-fill text-danger' : 'bi-shield-shaded text-primary'}`}></i>
                          {p.name}
                        </div>
                        {p.isSystemPolicy && <span className="badge bg-danger-subtle text-danger border border-danger-subtle small">Sistema</span>}
                      </div>
                      <div className="card-body">
                        <p className="small text-secondary mb-3">{p.description || 'Nessuna descrizione.'}</p>
                        <div className="mb-2 fw-semibold small text-dark">Permessi inclusi ({p.permissions.length}):</div>
                        <div className="d-flex flex-wrap gap-1">
                          {p.permissions.map((perm) => (
                            <span key={perm.id} className="badge bg-light text-dark border" title={perm.description}>
                              {perm.key}
                            </span>
                          ))}
                        </div>
                      </div>
                      <div className="card-footer bg-white border-top-0 d-flex justify-content-end gap-2 py-3">
                        <button className="btn btn-sm btn-outline-primary" onClick={() => handleOpenEditPolicy(p)}>
                          <i className="bi bi-pencil-square me-1"></i> Modifica
                        </button>
                        {!p.isSystemPolicy && (
                          <button className="btn btn-sm btn-outline-danger" onClick={() => handleDeletePolicy(p.id, p.name)}>
                            <i className="bi bi-trash"></i>
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* TAB 2: ASSEGNAZIONI UTENTI & SCOPE */}
        {activeTab === 'users' && (
          <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="text-muted small">Assegna le Policy agli utenti definendo lo Scope per Sottoazienda e Reparto.</span>
            </div>

            {loadingUsers ? (
              <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status"></div>
                <div className="text-muted small mt-2">Caricamento utenti e scope...</div>
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table table-hover align-middle">
                  <thead>
                    <tr className="text-secondary small">
                      <th>Utente</th>
                      <th>Email</th>
                      <th>Ruolo Principal</th>
                      <th>Policy Assegnate & Scope</th>
                      <th className="text-end">Azione</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.map((u) => {
                      const assignments = userAssignments[u.id] || [];
                      return (
                        <tr key={u.id}>
                          <td>
                            <div className="fw-bold text-dark">{u.fullName || u.username}</div>
                            <small className="text-muted">@{u.username}</small>
                          </td>
                          <td className="small text-muted">{u.email}</td>
                          <td>
                            <span className={`badge ${u.role === 'Admin' ? 'bg-danger' : u.role === 'Developer' ? 'bg-info text-dark' : 'bg-secondary'}`}>
                              {u.role}
                            </span>
                          </td>
                          <td>
                            {assignments.length === 0 ? (
                              <span className="badge bg-light text-muted border">Nessuna Policy diretta</span>
                            ) : (
                              <div className="d-flex flex-column gap-1">
                                {assignments.map((a) => (
                                  <div key={a.id} className="d-flex align-items-center gap-2 bg-light p-1.5 px-2 rounded border small">
                                    <span className="fw-semibold text-primary">{a.policyName}</span>
                                    <span className="badge bg-primary-subtle text-primary border">Azienda: {a.companyScope}</span>
                                    <span className="badge bg-success-subtle text-success border">Reparto: {a.departmentScope}</span>
                                    <button
                                      className="btn btn-sm text-danger p-0 ms-auto"
                                      title="Rimuovi Policy"
                                      onClick={() => handleRemoveUserAssignment(a.id)}
                                    >
                                      <i className="bi bi-x-circle-fill"></i>
                                    </button>
                                  </div>
                                ))}
                              </div>
                            )}
                          </td>
                          <td className="text-end">
                            <button className="btn btn-sm btn-outline-primary" onClick={() => handleOpenAssignModal(u)}>
                              <i className="bi bi-plus-lg me-1"></i> Assegna Policy
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Modal Creazione/Modifica Policy */}
      {showPolicyModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">
                  {editingPolicyId > 0 ? `Modifica Policy: ${policyName}` : 'Crea Nuova Policy Aziendale'}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowPolicyModal(false)}></button>
              </div>

              <form onSubmit={handleSavePolicy}>
                <div className="modal-body py-3">
                  <div className="mb-3">
                    <label className="form-label fw-semibold small text-secondary">Nome Policy</label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="es. Operatore Magazzino Pharmaself"
                      value={policyName}
                      onChange={(e) => setPolicyName(e.target.value)}
                      required
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label fw-semibold small text-secondary">Descrizione</label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Descrivi a quali reparti o moduli è destinata questa policy"
                      value={policyDescription}
                      onChange={(e) => setPolicyDescription(e.target.value)}
                    />
                  </div>

                  <div className="fw-bold text-dark mb-2">Seleziona Permessi Atomici da includere:</div>

                  {Object.keys(catalogByArea).length === 0 ? (
                    <div className="alert alert-warning small py-2 d-flex align-items-center justify-content-between">
                      <span>Nessun permesso atomico trovato nel catalogo. Ricaricamento in corso...</span>
                      <button type="button" className="btn btn-sm btn-outline-dark ms-2" onClick={loadPbacData}>
                        Ricarica Catalogo
                      </button>
                    </div>
                  ) : (
                    Object.keys(catalogByArea).map((area) => (
                    <div key={area} className="card mb-3 border border-light-subtle">
                      <div className="card-header bg-light py-2 fw-semibold text-primary small">
                        Area: {area}
                      </div>
                      <div className="card-body py-2">
                        <div className="row g-2">
                          {catalogByArea[area].map((perm) => {
                            const isChecked = selectedPermIds.includes(perm.id);
                            return (
                              <div key={perm.id} className="col-md-6">
                                <div className={`form-check p-2 rounded border ${isChecked ? 'bg-primary-subtle border-primary' : 'bg-light'}`}>
                                  <input
                                    className="form-check-input ms-0 me-2"
                                    type="checkbox"
                                    id={`perm-${perm.id}`}
                                    checked={isChecked}
                                    onChange={() => handleTogglePerm(perm.id)}
                                  />
                                  <label className="form-check-label small text-dark fw-semibold" htmlFor={`perm-${perm.id}`}>
                                    {perm.key}
                                  </label>
                                  <div className="text-muted" style={{ fontSize: '0.72rem' }}>
                                    {perm.description}
                                  </div>
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      </div>
                    </div>
                  )))
                  }
                </div>

                <div className="modal-footer border-0 pt-0">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowPolicyModal(false)}>
                    Annulla
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={savingPolicy}>
                    {savingPolicy && <span className="spinner-border spinner-border-sm me-1"></span>}
                    Salva Policy
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Modal Assegnazione Policy a Utente con Scope */}
      {showAssignModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">
                  Assegna Policy a: {selectedUserName}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowAssignModal(false)}></button>
              </div>

              <form onSubmit={handleSaveUserAssignment}>
                <div className="modal-body py-3">
                  <div className="mb-3">
                    <label className="form-label fw-semibold small text-secondary">Seleziona Policy</label>
                    <select
                      className="form-select"
                      value={selectedPolicyId}
                      onChange={(e) => setSelectedPolicyId(Number(e.target.value))}
                    >
                      {policies.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="mb-3">
                    <label className="form-label fw-semibold small text-secondary">Scope Sottoazienda (Company)</label>
                    <select
                      className="form-select"
                      value={selectedCompanyScope}
                      onChange={(e) => setSelectedCompanyScope(e.target.value)}
                    >
                      <option value="ALL">Tutte le Sottoaziende (ALL)</option>
                      <option value="Pharmaself24">Pharmaself24</option>
                      <option value="Skriptkiosk">SkriptKiosk</option>
                    </select>
                    <div className="form-text small">
                      Permette di limitare la policy solo a una specifica sottoazienda (es. Pharmaself24).
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label fw-semibold small text-secondary">Scope Reparto / Dipartimento</label>
                    <select
                      className="form-select"
                      value={selectedDepartmentScope}
                      onChange={(e) => setSelectedDepartmentScope(e.target.value)}
                    >
                      {DEPARTMENTS.map((dept) => (
                        <option key={dept} value={dept === 'Tutti' ? 'ALL' : dept}>
                          {dept}
                        </option>
                      ))}
                    </select>
                    <div className="form-text small">
                      Specifica il reparto di appartenenza dell'utente (es. Magazzino).
                    </div>
                  </div>
                </div>

                <div className="modal-footer border-0 pt-0">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowAssignModal(false)}>
                    Annulla
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={savingAssign}>
                    {savingAssign && <span className="spinner-border spinner-border-sm me-1"></span>}
                    Conferma Assegnazione
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
