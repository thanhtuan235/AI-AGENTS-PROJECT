export interface LoginCredentials {
  username: string;
  password: string;
}

export interface AuthUser {
  sub: string;
  name: string;
  role: string;
  dept_id: string | number;
  exp: number;
}

export interface LoginResponse {
  token: string;
}
