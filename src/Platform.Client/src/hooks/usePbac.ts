import { useEffect, useState } from 'react';

export interface UserPermissionScope {
  permissionKey: string;
  companyScope: string;
  departmentScope: string;
}

export function usePbac() {
  const [permissions, setPermissions] = useState<UserPermissionScope[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/Account/User')
      .then((res) => res.ok ? res.json() : null)
      .then(() => {
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  const can = (permissionKey: string, companyId?: string, departmentId?: string): boolean => {
    // Se non ci sono permessi caricati ma l'utente è admin, permission handler lato server garantisce comunque l'accesso.
    if (permissions.length === 0) return true;

    return permissions.some((p) => {
      if (p.permissionKey !== permissionKey && p.permissionKey !== '*') return false;

      const companyMatch = p.companyScope === 'ALL' || !companyId || p.companyScope.toLowerCase() === companyId.toLowerCase();
      const departmentMatch = p.departmentScope === 'ALL' || !departmentId || p.departmentScope.toLowerCase() === departmentId.toLowerCase();

      return companyMatch && departmentMatch;
    });
  };

  return { can, loading, permissions };
}
