import { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/lib/auth-store';

interface RequirePermProps {
  children: ReactNode;
  perm?: string;
  anyPerm?: string[];
  fallback?: string;
}

export function RequirePerm({ children, perm, anyPerm, fallback = '/tezgah' }: RequirePermProps) {
  const { hasPerm, hasAnyPerm } = useAuthStore();
  
  if (perm && !hasPerm(perm)) {
    return <Navigate to={fallback} replace />;
  }
  
  if (anyPerm && !hasAnyPerm(anyPerm)) {
    return <Navigate to={fallback} replace />;
  }
  
  return <>{children}</>;
}
