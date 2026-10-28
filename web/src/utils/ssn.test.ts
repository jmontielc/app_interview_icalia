import { describe, expect, it } from 'vitest';
import { formatSsnInput, validateSsn } from './ssn';

describe('validateSsn', () => {
  it.each(['123-45-6789', '123 45 6789', '123456789'])('accepts %s', (input) => {
    expect(validateSsn(input)).toBeNull();
  });

  it.each([
    ['', 'empty'],
    ['12345678', 'format'],
    ['123-45-678A', 'format'],
    ['000-12-3456', 'area'],
    ['666-12-3456', 'area'],
    ['987-65-4320', 'area'],
    ['123-00-4567', 'group'],
    ['123-45-0000', 'serial'],
    ['078-05-1120', 'known'],
  ])('rejects %s as %s', (input, expected) => {
    expect(validateSsn(input)).toBe(expected);
  });
});

describe('formatSsnInput', () => {
  it('inserts dashes as digits are typed', () => {
    expect(formatSsnInput('1')).toBe('1');
    expect(formatSsnInput('12345')).toBe('123-45');
    expect(formatSsnInput('123456789')).toBe('123-45-6789');
  });

  it('drops non-digits and caps at nine digits', () => {
    expect(formatSsnInput('12a3-45-6789999')).toBe('123-45-6789');
  });
});
