export type UserDto = {
  userId: string;
  username: string;
  displayName: string;
  roles: string[];
  isDisabled: boolean;
};

export type AdminCreateUserRequest = {
  username: string;
  displayName: string;
  password: string;
  roles: string[];
};

export type AdminUpdateUserRequest = {
  displayName: string;
  password?: string | null;
  isDisabled?: boolean | null;
};

export type AdminAssignRolesRequest = {
  roles: string[];
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
