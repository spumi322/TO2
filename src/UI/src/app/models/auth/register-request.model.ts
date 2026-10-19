export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  tenantName: string;
  inviteCode?: string;  // required only to join an existing organization
}
