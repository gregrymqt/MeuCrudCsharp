import type { User } from '../../../../types/models';

export type UserProfile = Pick<User, 'name' | 'email' | 'avatarUrl'>;
