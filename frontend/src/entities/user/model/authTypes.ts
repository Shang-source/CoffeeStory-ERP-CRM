import type { UserRole } from '@/entities/types';
import type { components } from '@/shared/api/generated/schema';
import type { ApiResponse } from '@/shared/api/openapi';

type RequireKeys<T, K extends keyof T> = T & { [P in K]-?: NonNullable<T[P]> };
type ApiSchemas = components['schemas'];

export type UserProfile = RequireKeys<ApiSchemas['UserProfileDto'], 'id' | 'email' | 'role' | 'name'> & {
  role: UserRole;
  customerId?: string | null;
};

export type LoginResponse = RequireKeys<Omit<ApiResponse<'/api/auth/login', 'post'>, 'role' | 'userProfile'>, 'accessToken' | 'expiresIn'> & {
  role: UserRole;
  userProfile: UserProfile;
};
