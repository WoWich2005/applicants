import { Navigate, Outlet, Route, Routes } from 'react-router'
import Results from './pages/Results'
import Applicants from './pages/Applicants'
import ApplicantEdit from './pages/ApplicantEdit'
import Faculties from './pages/Faculties'
import { ROUTES } from './constants/routes'
import Specialties from './pages/Specialties'
import SpecialtyEdit from './pages/SpecialtyEdit'
import EvaluationCriteria from './pages/EvaluationCriteria'
import EvaluationCriteriaGroups from './pages/EvaluationCriteriaGroups'
import EvaluationCriteriaGroupEdit from './pages/EvaluationCriteriaGroupEdit'
import Departments from './pages/Departments'
import CompetitionListEdit from './pages/CompetitionListEdit'
import Login from './pages/Login'
import Users from './pages/Users'
import MainContainer from './components/MainContainer'
import ProtectedRoute from './components/ProtectedRoute'
import { useAuth } from './contexts/AuthContext'

const ALL_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']
const ADMIN_ROLES = ['SuperAdmin']
const VIEW_ALL_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']

function AuthenticatedLayout() {
  const { auth } = useAuth()
  if (!auth) return <Navigate to={ROUTES.LOGIN} replace />
  return (
    <MainContainer>
      <Outlet />
    </MainContainer>
  )
}

export default function AppRouter() {
  return (
    <Routes>
      <Route path={ROUTES.LOGIN} element={<Login />} />

      <Route element={<AuthenticatedLayout />}>
        <Route path={ROUTES.RESULTS} element={<Results />} />

        <Route path={ROUTES.APPLICANTS} element={
          <ProtectedRoute roles={ALL_ROLES}><Applicants /></ProtectedRoute>
        } />
        <Route path={ROUTES.APPLICANT_EDIT} element={
          <ProtectedRoute roles={ALL_ROLES}><ApplicantEdit /></ProtectedRoute>
        } />

        <Route path={ROUTES.FACULTIES} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><Faculties /></ProtectedRoute>
        } />
        <Route path={ROUTES.DEPARTMENTS} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><Departments /></ProtectedRoute>
        } />
        <Route path={ROUTES.SPECIALTIES} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><Specialties /></ProtectedRoute>
        } />
        <Route path={ROUTES.SPECIALTY_EDIT} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><SpecialtyEdit /></ProtectedRoute>
        } />

        <Route path={ROUTES.EVALUATION_CRITERIA} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><EvaluationCriteria /></ProtectedRoute>
        } />
        <Route path={ROUTES.EVALUATION_CRITERIA_GROUPS} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><EvaluationCriteriaGroups /></ProtectedRoute>
        } />
        <Route path={ROUTES.EVALUATION_CRITERIA_GROUP_EDIT} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><EvaluationCriteriaGroupEdit /></ProtectedRoute>
        } />

        <Route path={ROUTES.COMPETITION_LIST_EDIT} element={
          <ProtectedRoute roles={VIEW_ALL_ROLES}><CompetitionListEdit /></ProtectedRoute>
        } />

        <Route path={ROUTES.USERS} element={
          <ProtectedRoute roles={ADMIN_ROLES}><Users /></ProtectedRoute>
        } />

        <Route path="*" element={<Navigate to={ROUTES.RESULTS} replace />} />
      </Route>
    </Routes>
  )
}
