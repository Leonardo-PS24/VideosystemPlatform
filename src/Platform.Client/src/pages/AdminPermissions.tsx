import { useEffect, useState } from 'react';

const COMPANIES = [
  {
    id: 'Pharmaself24',
    name: 'Pharmaself24',
    masterPermission: 'Company_Pharmaself24',
    icon: 'bi-shop',
    applications: ['ConfigurationKiosk']
  },
  {
    id: 'Skriptkiosk',
    name: 'SkriptKiosk',
    masterPermission: 'Company_Skriptkiosk',
    icon: 'bi-cpu',
    applications: ['SkriptkioskChecklist']
  }
];


interface ApplicationPermissionItem {
  applicationName: string;
  displayName: string;
  icon: string;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

interface UserPermissionsData {
  userId: string;
  username: string;
  fullName: string;
  email: string;
  role: string;
  applications: ApplicationPermissionItem[];
}

interface UserMatrixItem {
  userId: string;
  username: string;
  fullName: string;
  email: string;
  role: string;
  permissions?: Record<string, {
    permissionId: number;
    canView: boolean;
    canCreate: boolean;
    canEdit: boolean;
    canDelete: boolean;
    hasAnyPermission: boolean;
  }>;
}

interface RoleMatrixItem {
  roleName: string;
  applications: Record<string, {
    canView: boolean;
    canCreate: boolean;
    canEdit: boolean;
    canDelete: boolean;
  }>;
}

export default function AdminPermissions() {
  const [activeTab, setActiveTab] = useState<'users' | 'roles'>('users');
  
  // User Tab states
  const [users, setUsers] = useState<UserMatrixItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Role Tab states
  const [roles, setRoles] = useState<RoleMatrixItem[]>([]);
  const [loadingRoles, setLoadingRoles] = useState(false);

  // New Role Modal states
  const [showCreateRoleModal, setShowCreateRoleModal] = useState(false);
  const [newRoleName, setNewRoleName] = useState('');
  const [newRoleDescription, setNewRoleDescription] = useState('');
  const [creatingRole, setCreatingRole] = useState(false);
  const [createRoleError, setCreateRoleError] = useState<string | null>(null);

  // User Detail Modal states
  const [showModal, setShowModal] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserPermissionsData | null>(null);
  
  // Role Detail Modal states
  const [showRoleModal, setShowRoleModal] = useState(false);
  const [selectedRole, setSelectedRole] = useState<{
    roleName: string;
    applications: ApplicationPermissionItem[];
  } | null>(null);

  const [saving, setSaving] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);
  const [modalSuccess, setModalSuccess] = useState<string | null>(null);

  const loadMatrixData = () => {
    fetch('/api/Permissions')
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento della matrice dei permessi.');
        return res.json();
      })
      .then((data) => {
        setUsers(data.users);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setError('Impossibile caricare i permessi degli utenti.');
        setLoading(false);
      });
  };

  const loadRolesData = () => {
    setLoadingRoles(true);
    fetch('/api/Permissions/Roles')
      .then((res) => {
        if (!res.ok) throw new Error('Errore nel caricamento dei permessi dei ruoli.');
        return res.json();
      })
      .then((data) => {
        setRoles(data.roles);
        setLoadingRoles(false);
      })
      .catch((err) => {
        console.error(err);
        setLoadingRoles(false);
      });
  };

  useEffect(() => {
    loadMatrixData();
  }, []);

  const handleOpenPermissions = async (userId: string) => {
    setModalError(null);
    setModalSuccess(null);
    setShowModal(true);
    setSelectedUser(null);

    try {
      const response = await fetch(`/api/Permissions/User/${userId}`);
      if (response.ok) {
        const data = await response.json();
        setSelectedUser(data);
      } else {
        setModalError('Impossibile caricare i permessi di questo utente.');
      }
    } catch (err) {
      console.error(err);
      setModalError('Errore di connessione.');
    }
  };

  const handleOpenRolePermissions = async (roleName: string) => {
    setModalError(null);
    setModalSuccess(null);
    setShowRoleModal(true);
    setSelectedRole(null);

    try {
      const response = await fetch(`/api/Permissions/Role/${roleName}`);
      if (response.ok) {
        const data = await response.json();
        setSelectedRole(data);
      } else {
        setModalError('Impossibile caricare i permessi di questo ruolo.');
      }
    } catch (err) {
      console.error(err);
      setModalError('Errore di connessione.');
    }
  };

  const handlePermissionToggle = (appName: string, key: 'canView' | 'canCreate' | 'canEdit' | 'canDelete') => {
    if (!selectedUser) return;

    let updatedApps = selectedUser.applications.map((app) => {
      if (app.applicationName === appName) {
        const nextValue = !app[key];
        
        let adjustments = { [key]: nextValue };
        if (key === 'canView' && !nextValue) {
          adjustments = { canView: false, canCreate: false, canEdit: false, canDelete: false };
        } else if ((key === 'canCreate' || key === 'canEdit' || key === 'canDelete') && nextValue) {
          adjustments = { ...adjustments, canView: true };
        }

        return { ...app, ...adjustments };
      }
      return app;
    });

    const company = COMPANIES.find(c => c.masterPermission === appName);
    if (company && key === 'canView') {
      const isCompanyEnabled = updatedApps.find(a => a.applicationName === appName)?.canView ?? false;
      if (!isCompanyEnabled) {
        updatedApps = updatedApps.map((app) => {
          if (company.applications.includes(app.applicationName)) {
            return {
              ...app,
              canView: false,
              canCreate: false,
              canEdit: false,
              canDelete: false
            };
          }
          return app;
        });
      }
    }

    setSelectedUser({ ...selectedUser, applications: updatedApps });
  };

  const handleRolePermissionToggle = (appName: string, key: 'canView' | 'canCreate' | 'canEdit' | 'canDelete') => {
    if (!selectedRole) return;

    let updatedApps = selectedRole.applications.map((app) => {
      if (app.applicationName === appName) {
        const nextValue = !app[key];
        
        let adjustments = { [key]: nextValue };
        if (key === 'canView' && !nextValue) {
          adjustments = { canView: false, canCreate: false, canEdit: false, canDelete: false };
        } else if ((key === 'canCreate' || key === 'canEdit' || key === 'canDelete') && nextValue) {
          adjustments = { ...adjustments, canView: true };
        }

        return { ...app, ...adjustments };
      }
      return app;
    });

    const company = COMPANIES.find(c => c.masterPermission === appName);
    if (company && key === 'canView') {
      const isCompanyEnabled = updatedApps.find(a => a.applicationName === appName)?.canView ?? false;
      if (!isCompanyEnabled) {
        updatedApps = updatedApps.map((app) => {
          if (company.applications.includes(app.applicationName)) {
            return {
              ...app,
              canView: false,
              canCreate: false,
              canEdit: false,
              canDelete: false
            };
          }
          return app;
        });
      }
    }

    setSelectedRole({ ...selectedRole, applications: updatedApps });
  };

  const handleSavePermissions = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedUser) return;

    setSaving(true);
    setModalError(null);
    setModalSuccess(null);

    try {
      const response = await fetch(`/api/Permissions/User/${selectedUser.userId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(selectedUser)
      });

      if (response.ok) {
        setModalSuccess('Permessi personalizzati salvati con successo!');
        loadMatrixData();
        setTimeout(() => setShowModal(false), 1200);
      } else {
        setModalError('Impossibile salvare i permessi.');
      }
    } catch (err) {
      console.error(err);
      setModalError('Errore di connessione.');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveRolePermissions = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRole) return;

    setSaving(true);
    setModalError(null);
    setModalSuccess(null);

    try {
      const response = await fetch(`/api/Permissions/Role/${selectedRole.roleName}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ applications: selectedRole.applications })
      });

      if (response.ok) {
        setModalSuccess('Permessi di ruolo salvati con successo!');
        loadRolesData();
        loadMatrixData(); // ricarica gli utenti perché ereditano i nuovi permessi
        setTimeout(() => setShowRoleModal(false), 1200);
      } else {
        setModalError('Impossibile salvare i permessi del ruolo.');
      }
    } catch (err) {
      console.error(err);
      setModalError('Errore di connessione.');
    } finally {
      setSaving(false);
    }
  };

  const handleResetAll = async (userId: string, username: string) => {
    if (!window.confirm(`Sei sicuro di voler revocare l'override per l'utente ${username}? Tornerà ad ereditare i permessi di reparto.`)) return;

    try {
      const response = await fetch('/api/Permissions/Reset', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ userId })
      });

      if (response.ok) {
        alert('Override rimosso con successo. L\'utente eredita i permessi di default.');
        loadMatrixData();
      } else {
        alert('Impossibile resettare i permessi.');
      }
    } catch (err) {
      console.error(err);
    }
  };

  const hasOverride = (u: UserMatrixItem) => {
    return Object.values(u.permissions || {}).some(p => p.permissionId > 0);
  };

  const handleCreateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newRoleName.trim()) return;

    setCreatingRole(true);
    setCreateRoleError(null);

    try {
      const response = await fetch('/api/Permissions/CreateRole', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ roleName: newRoleName.trim(), description: newRoleDescription.trim() })
      });

      if (response.ok) {
        setShowCreateRoleModal(false);
        setNewRoleName('');
        setNewRoleDescription('');
        loadRolesData();
      } else {
        const errData = await response.json();
        setCreateRoleError(errData.message || 'Impossibile creare il ruolo.');
      }
    } catch (err) {
      console.error(err);
      setCreateRoleError('Errore di connessione.');
    } finally {
      setCreatingRole(false);
    }
  };

  const handleDeleteRole = async (roleName: string) => {
    if (!window.confirm(`Sei sicuro di voler eliminare il ruolo '${roleName}'?`)) return;

    try {
      const response = await fetch(`/api/Permissions/Role/${encodeURIComponent(roleName)}`, {
        method: 'DELETE'
      });

      if (response.ok) {
        loadRolesData();
      } else {
        const errData = await response.json();
        alert(errData.message || 'Impossibile eliminare il ruolo.');
      }
    } catch (err) {
      console.error(err);
      alert('Errore di connessione.');
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
        <h5 className="fw-bold mb-4 text-dark">Gestione Permessi Applicativi</h5>

        {/* Tab Selection */}
        <ul className="nav nav-tabs mb-4">
          <li className="nav-item">
            <button 
              className={`nav-link fw-semibold d-flex align-items-center gap-2 ${activeTab === 'users' ? 'active text-primary' : 'text-secondary'}`}
              onClick={() => setActiveTab('users')}
            >
              <i className="bi bi-people-fill"></i> Eccezioni Utenti (Override)
            </button>
          </li>
          <li className="nav-item">
            <button 
              className={`nav-link fw-semibold d-flex align-items-center gap-2 ${activeTab === 'roles' ? 'active text-primary' : 'text-secondary'}`}
              onClick={() => { setActiveTab('roles'); loadRolesData(); }}
            >
              <i className="bi bi-shield-lock-fill"></i> Permessi di Reparto (Ruoli)
            </button>
          </li>
        </ul>

        {/* TAB UTENTI */}
        {activeTab === 'users' && (
          <div className="table-responsive">
            <table className="table table-hover align-middle">
              <thead>
                <tr className="text-secondary small">
                  <th>Nome Utente</th>
                  <th>Email</th>
                  <th>Ruolo</th>
                  <th>Tipo Permesso</th>
                  <th className="text-end">Azioni</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.userId}>
                    <td>
                      <div className="fw-bold text-dark">{u.fullName || u.username}</div>
                    </td>
                    <td className="small text-muted">{u.email}</td>
                    <td>
                      <span className={`badge ${u.role === 'Admin' ? 'bg-danger' : u.role === 'Developer' ? 'bg-info text-dark' : 'bg-secondary'}`}>
                        {u.role}
                      </span>
                    </td>
                    <td>
                      {hasOverride(u) ? (
                        <span className="badge bg-warning text-dark">Override Personalizzato</span>
                      ) : (
                        <span className="badge bg-light text-muted border">Ereditato da Ruolo</span>
                      )}
                    </td>
                    <td className="text-end">
                      <div className="btn-group btn-group-sm">
                        <button 
                          onClick={() => handleOpenPermissions(u.userId)}
                          className="btn btn-outline-primary d-flex align-items-center gap-1"
                          title="Imposta Override"
                        >
                          <i className="bi bi-pencil-square"></i> Personalizza
                        </button>
                        {hasOverride(u) && (
                          <button 
                            onClick={() => handleResetAll(u.userId, u.username)}
                            className="btn btn-outline-danger"
                            title="Ripristina Ereditarietà Ruolo"
                          >
                            <i className="bi bi-arrow-counterclockwise"></i> Ripristina Ruolo
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* TAB RUOLI */}
        {activeTab === 'roles' && (
          <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="text-muted small">Gestisci i permessi predefiniti per ciascun ruolo o crea ruoli personalizzati.</span>
              <button 
                className="btn btn-sm btn-primary d-flex align-items-center gap-1"
                onClick={() => {
                  setNewRoleName('');
                  setNewRoleDescription('');
                  setCreateRoleError(null);
                  setShowCreateRoleModal(true);
                }}
              >
                <i className="bi bi-plus-circle-fill"></i> Crea Nuovo Ruolo
              </button>
            </div>

            <div className="table-responsive">
              {loadingRoles ? (
                <div className="text-center py-4">
                  <div className="spinner-border text-primary spinner-border-sm" role="status"></div>
                  <div className="text-muted small mt-2">Caricamento ruoli...</div>
                </div>
              ) : (
                <table className="table table-hover align-middle">
                  <thead>
                    <tr className="text-secondary small">
                      <th>Nome Ruolo (Reparto)</th>
                      <th className="text-end">Azioni</th>
                    </tr>
                  </thead>
                  <tbody>
                    {roles.map((r) => (
                      <tr key={r.roleName}>
                        <td>
                          <div className="fw-bold text-dark">{r.roleName}</div>
                        </td>
                        <td className="text-end">
                          <div className="btn-group btn-group-sm">
                            <button 
                              onClick={() => handleOpenRolePermissions(r.roleName)}
                              className="btn btn-outline-primary d-flex align-items-center gap-1"
                              title="Modifica Permessi Base Ruolo"
                              disabled={r.roleName === 'Admin'}
                            >
                              <i className="bi bi-shield-lock-fill"></i> Modifica Permessi
                            </button>
                            {!['Admin', 'Developer', 'User'].includes(r.roleName) && (
                              <button 
                                onClick={() => handleDeleteRole(r.roleName)}
                                className="btn btn-outline-danger"
                                title="Elimina Ruolo Personalizzato"
                              >
                                <i className="bi bi-trash-fill"></i> Elimina
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Modal Gestione Permessi Singoli */}
      {showModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">
                  Personalizza Eccezioni Permessi per: {selectedUser ? selectedUser.fullName || selectedUser.username : 'Caricamento...'}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>

              {selectedUser ? (
                <form onSubmit={handleSavePermissions}>
                  <div className="modal-body py-3">
                    {modalError && <div className="alert alert-danger py-2 small mb-3">{modalError}</div>}
                    {modalSuccess && <div className="alert alert-success py-2 small mb-3">{modalSuccess}</div>}

                    <div className="alert alert-info py-2 small mb-3">
                      <i className="bi bi-info-circle-fill me-1"></i>
                      Salvare questi permessi creerà un <strong>Override</strong> specifico che sovrascriverà completamente i permessi del ruolo di reparto <strong>{selectedUser.role}</strong>.
                    </div>

                    <div className="permissions-containers">
                      {COMPANIES.map((company) => {
                        const masterApp = selectedUser.applications.find(a => a.applicationName === company.masterPermission);
                        const isCompanyEnabled = masterApp?.canView ?? false;
                        const childApps = selectedUser.applications.filter(a => company.applications.includes(a.applicationName));

                        return (
                          <div key={company.id} className="card shadow-sm border border-light-subtle rounded-3 mb-4 overflow-hidden">
                            {/* Header sottoazienda */}
                            <div className="card-header bg-light d-flex align-items-center justify-content-between py-3 px-4">
                              <div className="d-flex align-items-center gap-3">
                                <div className="p-2 rounded-circle bg-white shadow-sm border border-light-subtle d-flex align-items-center justify-content-center" style={{ width: '40px', height: '40px' }}>
                                  <i className={`bi ${company.icon} text-primary fs-5`}></i>
                                </div>
                                <div>
                                  <h6 className="mb-0 fw-bold text-dark">{company.name}</h6>
                                  <small className="text-muted">Gestione abilitazione sottoazienda e moduli</small>
                                </div>
                              </div>
                              <div className="form-check form-switch mb-0">
                                <input
                                  className="form-check-input"
                                  type="checkbox"
                                  role="switch"
                                  id={`switch-user-${company.id}`}
                                  style={{ width: '2.5em', height: '1.25em', cursor: 'pointer' }}
                                  checked={isCompanyEnabled}
                                  onChange={() => handlePermissionToggle(company.masterPermission, 'canView')}
                                />
                                <label className="form-check-label ms-2 fw-semibold small text-secondary" htmlFor={`switch-user-${company.id}`}>
                                  {isCompanyEnabled ? 'Attiva' : 'Disattiva'}
                                </label>
                              </div>
                            </div>

                            {/* Body sottoazienda */}
                            <div 
                              className="card-body p-0 transition-all"
                              style={!isCompanyEnabled ? { opacity: 0.5, pointerEvents: 'none', filter: 'grayscale(0.85)', transition: 'all 0.3s ease' } : { transition: 'all 0.3s ease' }}
                            >
                              <div className="table-responsive">
                                <table className="table table-hover align-middle mb-0">
                                  <thead>
                                    <tr className="bg-light-subtle text-secondary small text-uppercase fw-semibold" style={{ fontSize: '0.72rem' }}>
                                      <th className="ps-4 py-3">Applicazione</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Visualizza</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Crea</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Modifica</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Elimina</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {childApps.map((app) => (
                                      <tr key={app.applicationName} className="border-bottom border-light-subtle">
                                        <td className="ps-4 py-3">
                                          <div className="d-flex align-items-center gap-3">
                                            <div className="p-2 rounded-2 bg-light text-secondary d-flex align-items-center justify-content-center" style={{ width: '36px', height: '36px' }}>
                                              <span className="material-icons fs-5">{app.icon || 'apps'}</span>
                                            </div>
                                            <div>
                                              <div className="fw-semibold text-dark small">{app.displayName}</div>
                                              <div className="text-muted small" style={{ fontSize: '0.7rem' }}>{app.applicationName}</div>
                                            </div>
                                          </div>
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canView}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canView')}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canCreate}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canCreate')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canEdit}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canEdit')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canDelete}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canDelete')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              </div>
                            </div>
                          </div>
                        );
                      })}

                      {/* Fallback per applicazioni non appartenenti a sottoaziende */}
                      {selectedUser.applications.filter(
                        a => !a.applicationName.startsWith('Company_') && 
                             !COMPANIES.some(c => c.applications.includes(a.applicationName))
                      ).length > 0 && (
                        <div className="card shadow-sm border border-light-subtle rounded-3 mb-4 overflow-hidden">
                          <div className="card-header bg-light py-3 px-4">
                            <h6 className="mb-0 fw-bold text-dark text-uppercase fw-bold text-secondary" style={{ fontSize: '0.75rem' }}>Altre Applicazioni</h6>
                          </div>
                          <div className="card-body p-0">
                            <div className="table-responsive">
                              <table className="table table-hover align-middle mb-0">
                                <thead>
                                  <tr className="bg-light-subtle text-secondary small text-uppercase fw-semibold" style={{ fontSize: '0.72rem' }}>
                                    <th className="ps-4 py-3">Applicazione</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Visualizza</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Crea</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Modifica</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Elimina</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {selectedUser.applications
                                    .filter(a => !a.applicationName.startsWith('Company_') && !COMPANIES.some(c => c.applications.includes(a.applicationName)))
                                    .map((app) => (
                                      <tr key={app.applicationName} className="border-bottom border-light-subtle">
                                        <td className="ps-4 py-3">
                                          <div className="d-flex align-items-center gap-3">
                                            <div className="p-2 rounded-2 bg-light text-secondary d-flex align-items-center justify-content-center" style={{ width: '36px', height: '36px' }}>
                                              <span className="material-icons fs-5">{app.icon || 'apps'}</span>
                                            </div>
                                            <div>
                                              <div className="fw-semibold text-dark small">{app.displayName}</div>
                                              <div className="text-muted small" style={{ fontSize: '0.7rem' }}>{app.applicationName}</div>
                                            </div>
                                          </div>
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canView}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canView')}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canCreate}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canCreate')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canEdit}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canEdit')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canDelete}
                                            onChange={() => handlePermissionToggle(app.applicationName, 'canDelete')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                      </tr>
                                    ))}
                                </tbody>
                              </table>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="modal-footer border-0 pt-0">
                    <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                      Annulla
                    </button>
                    <button type="submit" className="btn btn-primary" disabled={saving}>
                      {saving ? <span className="spinner-border spinner-border-sm me-1"></span> : null}
                      Salva Override Utente
                    </button>
                  </div>
                </form>
              ) : (
                <div className="modal-body text-center py-5">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Caricamento...</span>
                  </div>
                  <div className="text-muted small mt-2">Caricamento permessi dell'utente...</div>
                </div>
              )}

            </div>
          </div>
        </div>
      )}

      {/* Modal Gestione Permessi Ruolo */}
      {showRoleModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">
                  Definisci Permessi Base per il Ruolo: {selectedRole ? selectedRole.roleName : 'Caricamento...'}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowRoleModal(false)}></button>
              </div>

              {selectedRole ? (
                <form onSubmit={handleSaveRolePermissions}>
                  <div className="modal-body py-3">
                    {modalError && <div className="alert alert-danger py-2 small mb-3">{modalError}</div>}
                    {modalSuccess && <div className="alert alert-success py-2 small mb-3">{modalSuccess}</div>}

                    <div className="alert alert-warning py-2 small mb-3">
                      <i className="bi bi-exclamation-triangle-fill me-1"></i>
                      Attenzione: Modificare i permessi del ruolo applicherà le modifiche a <strong>tutti gli utenti</strong> assegnati a questo reparto, a meno che non abbiano un override personalizzato attivo.
                    </div>

                    <div className="permissions-containers">
                      {COMPANIES.map((company) => {
                        const masterApp = selectedRole.applications.find(a => a.applicationName === company.masterPermission);
                        const isCompanyEnabled = masterApp?.canView ?? false;
                        const childApps = selectedRole.applications.filter(a => company.applications.includes(a.applicationName));

                        return (
                          <div key={company.id} className="card shadow-sm border border-light-subtle rounded-3 mb-4 overflow-hidden">
                            {/* Header sottoazienda */}
                            <div className="card-header bg-light d-flex align-items-center justify-content-between py-3 px-4">
                              <div className="d-flex align-items-center gap-3">
                                <div className="p-2 rounded-circle bg-white shadow-sm border border-light-subtle d-flex align-items-center justify-content-center" style={{ width: '40px', height: '40px' }}>
                                  <i className={`bi ${company.icon} text-primary fs-5`}></i>
                                </div>
                                <div>
                                  <h6 className="mb-0 fw-bold text-dark">{company.name}</h6>
                                  <small className="text-muted">Gestione abilitazione sottoazienda e moduli</small>
                                </div>
                              </div>
                              <div className="form-check form-switch mb-0">
                                <input
                                  className="form-check-input"
                                  type="checkbox"
                                  role="switch"
                                  id={`switch-role-${company.id}`}
                                  style={{ width: '2.5em', height: '1.25em', cursor: 'pointer' }}
                                  checked={isCompanyEnabled}
                                  onChange={() => handleRolePermissionToggle(company.masterPermission, 'canView')}
                                />
                                <label className="form-check-label ms-2 fw-semibold small text-secondary" htmlFor={`switch-role-${company.id}`}>
                                  {isCompanyEnabled ? 'Attiva' : 'Disattiva'}
                                </label>
                              </div>
                            </div>

                            {/* Body sottoazienda */}
                            <div 
                              className="card-body p-0 transition-all"
                              style={!isCompanyEnabled ? { opacity: 0.5, pointerEvents: 'none', filter: 'grayscale(0.85)', transition: 'all 0.3s ease' } : { transition: 'all 0.3s ease' }}
                            >
                              <div className="table-responsive">
                                <table className="table table-hover align-middle mb-0">
                                  <thead>
                                    <tr className="bg-light-subtle text-secondary small text-uppercase fw-semibold" style={{ fontSize: '0.72rem' }}>
                                      <th className="ps-4 py-3">Applicazione</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Visualizza</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Crea</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Modifica</th>
                                      <th className="text-center py-3" style={{ width: '90px' }}>Elimina</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {childApps.map((app) => (
                                      <tr key={app.applicationName} className="border-bottom border-light-subtle">
                                        <td className="ps-4 py-3">
                                          <div className="d-flex align-items-center gap-3">
                                            <div className="p-2 rounded-2 bg-light text-secondary d-flex align-items-center justify-content-center" style={{ width: '36px', height: '36px' }}>
                                              <span className="material-icons fs-5">{app.icon || 'apps'}</span>
                                            </div>
                                            <div>
                                              <div className="fw-semibold text-dark small">{app.displayName}</div>
                                              <div className="text-muted small" style={{ fontSize: '0.7rem' }}>{app.applicationName}</div>
                                            </div>
                                          </div>
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canView}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canView')}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canCreate}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canCreate')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canEdit}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canEdit')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canDelete}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canDelete')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              </div>
                            </div>
                          </div>
                        );
                      })}

                      {/* Fallback per applicazioni non appartenenti a sottoaziende */}
                      {selectedRole.applications.filter(
                        a => !a.applicationName.startsWith('Company_') && 
                             !COMPANIES.some(c => c.applications.includes(a.applicationName))
                      ).length > 0 && (
                        <div className="card shadow-sm border border-light-subtle rounded-3 mb-4 overflow-hidden">
                          <div className="card-header bg-light py-3 px-4">
                            <h6 className="mb-0 fw-bold text-dark text-uppercase fw-bold text-secondary" style={{ fontSize: '0.75rem' }}>Altre Applicazioni</h6>
                          </div>
                          <div className="card-body p-0">
                            <div className="table-responsive">
                              <table className="table table-hover align-middle mb-0">
                                <thead>
                                  <tr className="bg-light-subtle text-secondary small text-uppercase fw-semibold" style={{ fontSize: '0.72rem' }}>
                                    <th className="ps-4 py-3">Applicazione</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Visualizza</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Crea</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Modifica</th>
                                    <th className="text-center py-3" style={{ width: '90px' }}>Elimina</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {selectedRole.applications
                                    .filter(a => !a.applicationName.startsWith('Company_') && !COMPANIES.some(c => c.applications.includes(a.applicationName)))
                                    .map((app) => (
                                      <tr key={app.applicationName} className="border-bottom border-light-subtle">
                                        <td className="ps-4 py-3">
                                          <div className="d-flex align-items-center gap-3">
                                            <div className="p-2 rounded-2 bg-light text-secondary d-flex align-items-center justify-content-center" style={{ width: '36px', height: '36px' }}>
                                              <span className="material-icons fs-5">{app.icon || 'apps'}</span>
                                            </div>
                                            <div>
                                              <div className="fw-semibold text-dark small">{app.displayName}</div>
                                              <div className="text-muted small" style={{ fontSize: '0.7rem' }}>{app.applicationName}</div>
                                            </div>
                                          </div>
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canView}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canView')}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canCreate}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canCreate')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canEdit}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canEdit')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                        <td className="text-center py-3">
                                          <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={app.canDelete}
                                            onChange={() => handleRolePermissionToggle(app.applicationName, 'canDelete')}
                                            disabled={!app.canView}
                                          />
                                        </td>
                                      </tr>
                                    ))}
                                </tbody>
                              </table>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="modal-footer border-0 pt-0">
                    <button type="button" className="btn btn-secondary" onClick={() => setShowRoleModal(false)}>
                      Annulla
                    </button>
                    <button type="submit" className="btn btn-primary" disabled={saving}>
                      {saving ? <span className="spinner-border spinner-border-sm me-1"></span> : null}
                      Salva Permessi Ruolo
                    </button>
                  </div>
                </form>
              ) : (
                <div className="modal-body text-center py-5">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Caricamento...</span>
                  </div>
                  <div className="text-muted small mt-2">Caricamento permessi del ruolo...</div>
                </div>
              )}

            </div>
          </div>
        </div>
      )}

      {/* Modal Creazione Nuovo Ruolo */}
      {showCreateRoleModal && (
        <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
              <div className="modal-header border-0 pb-0">
                <h5 className="modal-title fw-bold text-dark">
                  Crea Nuovo Ruolo Personalizzato
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowCreateRoleModal(false)}></button>
              </div>

              <form onSubmit={handleCreateRole}>
                <div className="modal-body py-3">
                  {createRoleError && <div className="alert alert-danger py-2 small mb-3">{createRoleError}</div>}

                  <div className="mb-3">
                    <label htmlFor="roleNameInput" className="form-label small fw-semibold text-secondary">
                      Nome Ruolo <span className="text-danger">*</span>
                    </label>
                    <input
                      id="roleNameInput"
                      type="text"
                      className="form-control"
                      placeholder="Es. Manager, Tecnico, Contabile"
                      value={newRoleName}
                      onChange={(e) => setNewRoleName(e.target.value)}
                      required
                    />
                  </div>

                  <div className="mb-3">
                    <label htmlFor="roleDescriptionInput" className="form-label small fw-semibold text-secondary">
                      Descrizione (Opzionale)
                    </label>
                    <textarea
                      id="roleDescriptionInput"
                      className="form-control"
                      rows={3}
                      placeholder="Descrizione delle responsabilità o del reparto..."
                      value={newRoleDescription}
                      onChange={(e) => setNewRoleDescription(e.target.value)}
                    ></textarea>
                  </div>
                </div>

                <div className="modal-footer border-0 pt-0">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowCreateRoleModal(false)}>
                    Annulla
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={creatingRole || !newRoleName.trim()}>
                    {creatingRole ? <span className="spinner-border spinner-border-sm me-1"></span> : null}
                    Crea Ruolo
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
