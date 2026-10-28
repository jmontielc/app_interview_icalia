<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { api, ApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';
import type {
  ApplicantResponse,
  Bureau,
  CreditReportResponse,
  DocumentResponse,
  DownloadLinkResponse,
  EmploymentResponse,
} from '@/api/types';

const props = defineProps<{ id: number }>();
const auth = useAuthStore();

const canOperate = computed(() => auth.hasAnyRole('Admin', 'LoanOfficer'));

const applicant = ref<ApplicantResponse | null>(null);
const loadError = ref<string | null>(null);
const bureau = ref<Bureau>('equifax');
const busy = ref<string | null>(null);
const actionError = ref<string | null>(null);
const creditReport = ref<CreditReportResponse | null>(null);
const employment = ref<EmploymentResponse | null>(null);
const documents = ref<DocumentResponse[]>([]);
const downloadLink = ref<DownloadLinkResponse | null>(null);
const selectedFile = ref<File | null>(null);

onMounted(load);

async function load() {
  try {
    applicant.value = await api<ApplicantResponse>(`/api/applicants/${props.id}`);
  } catch (err) {
    loadError.value = err instanceof ApiError ? err.message : 'Could not load the applicant.';
  }
}

async function run(label: string, action: () => Promise<void>) {
  actionError.value = null;
  busy.value = label;
  try {
    await action();
  } catch (err) {
    actionError.value = err instanceof ApiError ? err.message : 'The request failed.';
  } finally {
    busy.value = null;
  }
}

const runCreditCheck = () =>
  run('credit', async () => {
    creditReport.value = await api<CreditReportResponse>(
      `/api/credit-reports/${props.id}?bureau=${bureau.value}`,
      { method: 'POST' },
    );
  });

const runEmploymentCheck = () =>
  run('employment', async () => {
    employment.value = await api<EmploymentResponse>(
      `/api/applicants/${props.id}/employment-check?bureau=${bureau.value}`,
      { method: 'POST' },
    );
  });

function onFileChange(event: Event) {
  selectedFile.value = (event.target as HTMLInputElement).files?.[0] ?? null;
}

const uploadDocument = () =>
  run('upload', async () => {
    if (!selectedFile.value) {
      throw new ApiError(400, 'Choose a PDF file first.');
    }
    const form = new FormData();
    form.append('file', selectedFile.value);
    const created = await api<DocumentResponse>(`/api/documents?applicantId=${props.id}`, {
      method: 'POST',
      body: form,
    });
    documents.value = [created, ...documents.value];
    selectedFile.value = null;
  });

const refreshDocument = (doc: DocumentResponse) =>
  run('status', async () => {
    const latest = await api<DocumentResponse>(`/api/documents/${doc.id}`);
    documents.value = documents.value.map((d) => (d.id === latest.id ? latest : d));
  });

const openDocument = (doc: DocumentResponse) =>
  run('link', async () => {
    downloadLink.value = await api<DownloadLinkResponse>(`/api/documents/${doc.id}/url`);
    window.open(downloadLink.value.url, '_blank', 'noopener,noreferrer');
  });
</script>

<template>
  <section v-if="loadError" class="card">
    <p class="error" role="alert">{{ loadError }}</p>
  </section>

  <template v-else-if="applicant">
    <section class="card">
      <h1>{{ applicant.firstName }} {{ applicant.lastName }}</h1>
      <p>SSN: <code>{{ applicant.maskedSsn }}</code></p>
      <div class="field">
        <label for="bureau">Bureau</label>
        <select id="bureau" v-model="bureau">
          <option value="equifax">Equifax</option>
          <option value="transunion">TransUnion</option>
        </select>
      </div>
      <div v-if="canOperate">
        <button type="button" :disabled="busy !== null" @click="runCreditCheck">Run credit check</button>
        <button type="button" :disabled="busy !== null" @click="runEmploymentCheck">Verify employment</button>
      </div>
      <p v-else class="muted">Your role can view applicants and documents but cannot run checks.</p>
      <p v-if="actionError" class="error" role="alert">{{ actionError }}</p>
    </section>

    <section v-if="creditReport" class="card">
      <h2>Credit summary ({{ creditReport.bureau }})</h2>
      <p>Score <strong>{{ creditReport.score }}</strong> · Open accounts {{ creditReport.openAccounts }} · Total balance {{ creditReport.totalBalance }}</p>
    </section>

    <section v-if="employment" class="card">
      <h2>Employment ({{ employment.bureau }})</h2>
      <p v-if="employment.isEmployed">Currently employed<span v-if="employment.employerName"> at {{ employment.employerName }}</span>.</p>
      <p v-else>No active employment found.</p>
    </section>

    <section class="card">
      <h2>Documents</h2>
      <div v-if="canOperate" class="field">
        <input type="file" accept="application/pdf" @change="onFileChange" />
        <button type="button" :disabled="busy !== null || !selectedFile" @click="uploadDocument">Upload PDF</button>
      </div>
      <p v-if="documents.length === 0" class="muted">Documents uploaded in this session appear here.</p>
      <table v-else>
        <thead>
          <tr><th>File</th><th>Status</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="doc in documents" :key="doc.id">
            <td>{{ doc.fileName }}</td>
            <td>{{ doc.processingStatus }}</td>
            <td>
              <button type="button" :disabled="busy !== null" @click="refreshDocument(doc)">Refresh status</button>
              <button type="button" :disabled="busy !== null || doc.processingStatus !== 'completed'" @click="openDocument(doc)">
                Open
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </template>

  <p v-else class="muted">Loading…</p>
</template>
