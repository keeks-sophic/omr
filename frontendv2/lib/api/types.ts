export type UserDto = {
  userId: string;
  username: string;
  displayName: string;
  roles: string[];
  isDisabled: boolean;
};

export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  user: UserDto;
};

export type RegisterRequest = {
  username: string;
  displayName: string;
  password: string;
};

export type RegisterResponse = {
  userId: string;
};

export type MeResponse = {
  userId: string;
  username: string;
  roles: string[];
};

