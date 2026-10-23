import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { schoolApi } from '../services/endpoints';
import { useAuth } from './AuthContext';

const FALLBACK = {
  schoolName: 'School Management System',
  contactEmail: null,
  academicYear: '',
  allowSelfRegistration: false,
  heroSubtitle: 'Nursery through Grade 12, one portal.',
  heroDescription: 'Lessons, weighted assessments and report cards in one place.',
  primaryColor: '#1e3a8a',
};

const SchoolInfoContext = createContext(null);


export function SchoolInfoProvider({ children }) {
  const { user } = useAuth();
  const [info, setInfo] = useState(FALLBACK);
  const [loading, setLoading] = useState(true);
  const currentSchoolId = useRef(user?.schoolId);
  const requestVersion = useRef(0);

  const refresh = useCallback(async () => {
    const requestedSchoolId = user?.schoolId;
    const version = ++requestVersion.current;
    try {
      const data = await schoolApi.getInfo();
      if (version !== requestVersion.current || requestedSchoolId !== currentSchoolId.current)
        return null;
      setInfo({ ...FALLBACK, ...data });
      return data;
    } catch {
      if (version !== requestVersion.current || requestedSchoolId !== currentSchoolId.current)
        return null;
      setInfo(FALLBACK);
      return null;
    } finally {
      if (version === requestVersion.current && requestedSchoolId === currentSchoolId.current) {
        setLoading(false);
      }
    }
  }, [user?.schoolId]);

  useEffect(() => {
    currentSchoolId.current = user?.schoolId;
    setInfo(FALLBACK);
    setLoading(true);
    refresh();
    return () => {
      requestVersion.current += 1;
    };
  }, [refresh, user?.schoolId]);

  const value = useMemo(() => ({ ...info, loading, refresh }), [info, loading, refresh]);

  return <SchoolInfoContext.Provider value={value}>{children}</SchoolInfoContext.Provider>;
}

export function useSchoolInfo() {
  const context = useContext(SchoolInfoContext);
  if (!context) {
    throw new Error('useSchoolInfo must be used inside a <SchoolInfoProvider>.');
  }
  return context;
}
