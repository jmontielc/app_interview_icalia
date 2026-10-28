// Client-side SSN rules. These mirror src/Api/Security/SsnValidator.cs for fast feedback.
// The server is authoritative and validates again.

export type SsnError = 'empty' | 'format' | 'area' | 'group' | 'serial' | 'known';

const KNOWN_INVALID = new Set(['078051120', '219099999']);

export const SSN_ERROR_MESSAGES: Record<SsnError, string> = {
  empty: 'Enter the SSN.',
  format: 'Enter 9 digits, for example 123-45-6789.',
  area: 'This SSN area number is not valid.',
  group: 'This SSN group number is not valid.',
  serial: 'This SSN serial number is not valid.',
  known: 'This SSN is a published sample number and cannot be used.',
};

export function normalizeSsn(input: string): string {
  return input.replace(/[-\s]/g, '');
}

export function validateSsn(input: string): SsnError | null {
  if (input.trim() === '') {
    return 'empty';
  }

  const digits = normalizeSsn(input);
  if (!/^\d{9}$/.test(digits)) {
    return 'format';
  }

  const area = digits.slice(0, 3);
  if (area === '000' || area === '666' || area.startsWith('9')) {
    return 'area';
  }
  if (digits.slice(3, 5) === '00') {
    return 'group';
  }
  if (digits.slice(5) === '0000') {
    return 'serial';
  }
  if (KNOWN_INVALID.has(digits)) {
    return 'known';
  }

  return null;
}

/** Keeps digits only and inserts dashes as the user types: 123-45-6789. */
export function formatSsnInput(raw: string): string {
  const digits = raw.replace(/\D/g, '').slice(0, 9);
  if (digits.length <= 3) {
    return digits;
  }
  if (digits.length <= 5) {
    return `${digits.slice(0, 3)}-${digits.slice(3)}`;
  }
  return `${digits.slice(0, 3)}-${digits.slice(3, 5)}-${digits.slice(5)}`;
}
