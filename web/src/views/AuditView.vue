<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { api, ApiError } from '@/api/client';
import type { AuditEntryResponse } from '@/api/types';

const entries = ref<AuditEntryResponse[]>([]);
const error = ref<string | null>(null);

onMounted(async () => {
  try {
    entries.value = await api<AuditEntryResponse[]>('/api/audit');
  } catch (err) {
    error.value = err instanceof ApiError ? err.message : 'Could not load the audit log.';
  }
});
</script>

<template>
  <section class="card">
    <h1>Audit log</h1>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <table v-else>
      <thead>
        <tr><th>When</th><th>Who</th><th>Action</th><th>Target</th></tr>
      </thead>
      <tbody>
        <tr v-for="entry in entries" :key="entry.id">
          <td>{{ new Date(entry.occurredAt).toLocaleString() }}</td>
          <td>{{ entry.actor }}</td>
          <td>{{ entry.action }}</td>
          <td>{{ entry.target ?? '' }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>
