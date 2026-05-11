import { useAuth } from '../contexts/AuthContext'

export const ROLES = {
  SuperAdmin: 'SuperAdmin',
  DataAdministrator: 'DataAdministrator',
  Auditor: 'Auditor',
  AdmissionsOperator: 'AdmissionsOperator',
  DataViewer: 'DataViewer',
}

const WRITE_STRUCTURE = [ROLES.SuperAdmin, ROLES.DataAdministrator]
const WRITE_APPLICANTS = [ROLES.SuperAdmin, ROLES.DataAdministrator, ROLES.Auditor, ROLES.AdmissionsOperator]
const WRITE_AUDIT = [ROLES.SuperAdmin, ROLES.DataAdministrator, ROLES.Auditor]

export function usePermissions() {
  const { auth } = useAuth()
  const role = auth?.role

  return {
    canWriteStructure: WRITE_STRUCTURE.includes(role),
    canWriteApplicants: WRITE_APPLICANTS.includes(role),
    canWriteAudit: WRITE_AUDIT.includes(role),
    canManageUsers: role === ROLES.SuperAdmin,
    canRecalculate: WRITE_STRUCTURE.includes(role),
  }
}
