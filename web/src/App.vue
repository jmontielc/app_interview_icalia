<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, RouterLink, RouterView } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const auth = useAuthStore();
const router = useRouter();

const canCreate = computed(() => auth.hasAnyRole('Admin', 'LoanOfficer'));
const canAudit = computed(() => auth.hasAnyRole('Auditor', 'Admin'));

function signOut() {
  auth.clear();
  router.push({ name: 'login' });
}
</script>

<template>
  <header v-if="auth.isAuthenticated" class="topbar">
    <strong>Verification</strong>
    <nav>
      <RouterLink v-if="canCreate" to="/applicants/new">New applicant</RouterLink>
      <RouterLink v-if="canAudit" to="/audit">Audit log</RouterLink>
    </nav>
    <span class="who">{{ auth.username }} · {{ auth.role }}</span>
    <button type="button" @click="signOut">Sign out</button>
  </header>

  <main>
    <RouterView />
  </main>
</template>

<style>
:root {
  font-family: system-ui, sans-serif;
  color: #1d2733;
}

body {
  margin: 0;
}

.topbar {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.75rem 1.5rem;
  background: #1d2733;
  color: #fff;
}

.topbar a {
  color: #cfe3ff;
}

.topbar .who {
  margin-left: auto;
  font-size: 0.9rem;
}

main {
  max-width: 880px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.card {
  border: 1px solid #d6dde6;
  border-radius: 8px;
  padding: 1.25rem;
  margin-bottom: 1.25rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.9rem;
}

.error {
  color: #b00020;
  font-size: 0.9rem;
}

.muted {
  color: #5b6876;
  font-size: 0.9rem;
}

button {
  padding: 0.45rem 0.9rem;
  border-radius: 6px;
  border: 1px solid #1d2733;
  background: #1d2733;
  color: #fff;
  cursor: pointer;
}

button:disabled {
  opacity: 0.5;
  cursor: default;
}

input,
select {
  padding: 0.45rem;
  border: 1px solid #b8c3d0;
  border-radius: 6px;
  font: inherit;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  text-align: left;
  padding: 0.4rem;
  border-bottom: 1px solid #e3e8ef;
}
</style>
