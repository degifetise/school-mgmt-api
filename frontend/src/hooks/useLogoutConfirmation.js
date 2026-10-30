import { useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function useLogoutConfirmation() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const confirmLogout = useCallback(async () => {
    setIsLoggingOut(true);
    try {
      await logout({ silent: false });
      setIsOpen(false);
      navigate('/login', { replace: true });
    } finally {
      setIsLoggingOut(false);
    }
  }, [logout, navigate]);

  const requestLogout = useCallback(() => setIsOpen(true), []);
  const cancelLogout = useCallback(() => {
    if (!isLoggingOut) setIsOpen(false);
  }, [isLoggingOut]);

  return {
    isOpen,
    isLoggingOut,
    requestLogout,
    cancelLogout,
    confirmLogout,
  };
}
