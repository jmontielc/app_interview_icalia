// Mirrors the API response shapes in src/Api/Contracts. SSNs only ever arrive masked.

export type Bureau = 'equifax' | 'transunion';

export interface TokenResponse {
  accessToken: string;
  role: 'Admin' | 'LoanOfficer' | 'Auditor';
}

export interface ApplicantResponse {
  id: number;
  firstName: string;
  lastName: string;
  maskedSsn: string;
}

export interface CreditReportResponse {
  id: number;
  applicantId: number;
  bureau: string;
  score: number;
  openAccounts: number;
  totalBalance: number;
  retrievedAt: string;
}

export interface EmploymentResponse {
  applicantId: number;
  bureau: string;
  isEmployed: boolean;
  employerName: string | null;
  asOf: string;
}

export interface DocumentResponse {
  id: number;
  applicantId: number;
  fileName: string;
  sizeBytes: number;
  processingStatus: string;
  uploadedAt: string;
}

export interface DownloadLinkResponse {
  url: string;
  expiresInSeconds: number;
}

export interface AuditEntryResponse {
  id: number;
  occurredAt: string;
  actor: string;
  action: string;
  target: string | null;
}
