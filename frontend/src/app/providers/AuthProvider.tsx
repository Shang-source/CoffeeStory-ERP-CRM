import { createContext, useContext, useState, ReactNode } from 'react';
import { login as apiLogin } from '@/features/auth/api/authApi';
import type { UserProfile } from '@/entities/user/model/authTypes';
import { clearSession, getStoredUser } from '@/shared/api/sessionStorage';

interface AuthContextType {
  user: UserProfile | null;
  login: (email: string, password: string) => Promise<UserProfile | null>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(() => getStoredUser<UserProfile>());

  const login = async (email: string, password: string): Promise<UserProfile | null> => {
    try {
      const profile = await apiLogin(email, password);
      setUser(profile);
      return profile;
    } catch {
      clearSession();
      setUser(null);
      return null;
    }
  };

  const logout = () => {
    clearSession();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
