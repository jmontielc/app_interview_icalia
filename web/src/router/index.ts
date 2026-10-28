import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore, type Role } from '@/stores/auth';
import LoginView from '@/views/LoginView.vue';
import NewApplicantView from '@/views/NewApplicantView.vue';
import ApplicantDetailView from '@/views/ApplicantDetailView.vue';
import AuditView from '@/views/AuditView.vue';

declare module 'vue-router' {
  interface RouteMeta {
    public?: boolean;
    roles?: Role[];
  }
}

const ALL_ROLES: Role[] = ['Admin', 'LoanOfficer', 'Auditor'];

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/applicants/new' },
    { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
    {
      path: '/applicants/new',
      name: 'new-applicant',
      component: NewApplicantView,
      meta: { roles: ['Admin', 'LoanOfficer'] },
    },
    {
      path: '/applicants/:id(\\d+)',
      name: 'applicant',
      component: ApplicantDetailView,
      props: (route) => ({ id: Number(route.params.id) }),
      meta: { roles: ALL_ROLES },
    },
    {
      path: '/audit',
      name: 'audit',
      component: AuditView,
      meta: { roles: ['Auditor', 'Admin'] },
    },
  ],
});

// Route guards mirror the API policies. The API is the real control, and these checks only keep the UI consistent.
router.beforeEach((to) => {
  const auth = useAuthStore();

  if (to.meta.public) {
    return true;
  }

  if (!auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

  const allowed = to.meta.roles;
  if (allowed && !auth.hasAnyRole(...allowed)) {
    return { path: '/applicants/new' };
  }

  return true;
});
