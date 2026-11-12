export interface AuthResponse {
  userId: number;
  userName: string;
  email: string;
  tenantId: number;
  tenantName: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  tenantName: string;
  inviteCode?: string;
}

export interface OrganizationExistsResponse {
  exists: boolean;
}

export interface InviteCodeResponse {
  code: string;
  expiresAt: string;
}
