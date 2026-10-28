<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api, ApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';
import type { TokenResponse } from '@/api/types';

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();

const username = ref('');
const password = ref('');
const error = ref<string | null>(null);
const submitting = ref(false);

async function submit() {
  error.value = null;
  submitting.value = true;
  try {
    // Development-only endpoint. Production sign-in goes through the identity provider.
    const token = await api<TokenResponse>('/api/auth/token', {
      method: 'POST',
      body: { username: username.value, password: password.value },
    });
    auth.setSession(token.accessToken, token.role, username.value);
    password.value = '';
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/applicants/new';
    router.push(redirect);
  } catch (err) {
    error.value = err instanceof ApiError && err.status === 401
      ? 'Wrong username or password.'
      : 'Sign-in failed. Check that the API is running.';
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <section class="card">
    <h1>Sign in</h1>
    <form @submit.prevent="submit">
      <div class="field">
        <label for="username">Username</label>
        <input id="username" v-model="username" autocomplete="username" required />
      </div>
      <div class="field">
        <label for="password">Password</label>
        <input id="password" v-model="password" type="password" autocomplete="current-password" required />
      </div>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <button type="submit" :disabled="submitting">Sign in</button>
    </form>
    <p class="muted">
      Local development accounts: admin, loanofficer, auditor. The password is set in the API's development configuration.
    </p>
  </section>
</template>
