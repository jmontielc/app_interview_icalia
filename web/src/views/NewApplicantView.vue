<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { api, ApiError } from '@/api/client';
import type { ApplicantResponse } from '@/api/types';
import { SSN_ERROR_MESSAGES, formatSsnInput, validateSsn } from '@/utils/ssn';

const router = useRouter();

const firstName = ref('');
const lastName = ref('');
// The full SSN stays in this component only. It is cleared after submit and never stored in Pinia.
const ssn = ref('');
const submitError = ref<string | null>(null);
const submitting = ref(false);
const touched = ref(false);

const ssnError = computed(() => {
  const code = validateSsn(ssn.value);
  return code ? SSN_ERROR_MESSAGES[code] : null;
});

const canSubmit = computed(() => !submitting.value && firstName.value.trim() !== '' && lastName.value.trim() !== '' && !ssnError.value);

function onSsnInput(event: Event) {
  ssn.value = formatSsnInput((event.target as HTMLInputElement).value);
}

async function submit() {
  touched.value = true;
  if (!canSubmit.value) {
    return;
  }

  submitError.value = null;
  submitting.value = true;
  try {
    const applicant = await api<ApplicantResponse>('/api/applicants', {
      method: 'POST',
      body: { firstName: firstName.value.trim(), lastName: lastName.value.trim(), ssn: ssn.value },
    });
    ssn.value = '';
    router.push({ name: 'applicant', params: { id: applicant.id } });
  } catch (err) {
    submitError.value = err instanceof ApiError ? err.message : 'Could not save the applicant.';
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <section class="card">
    <h1>New applicant</h1>
    <form @submit.prevent="submit" novalidate>
      <div class="field">
        <label for="first">First name</label>
        <input id="first" v-model="firstName" autocomplete="off" />
      </div>
      <div class="field">
        <label for="last">Last name</label>
        <input id="last" v-model="lastName" autocomplete="off" />
      </div>
      <div class="field">
        <label for="ssn">Social Security number</label>
        <input
          id="ssn"
          :value="ssn"
          inputmode="numeric"
          autocomplete="off"
          placeholder="123-45-6789"
          maxlength="11"
          @input="onSsnInput"
        />
        <span v-if="touched && ssnError" class="error">{{ ssnError }}</span>
        <span v-else class="muted">Shown masked after saving. Only the last four digits are kept for display.</span>
      </div>
      <p v-if="submitError" class="error" role="alert">{{ submitError }}</p>
      <button type="submit" :disabled="submitting">Save applicant</button>
    </form>
  </section>
</template>
