import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { SchoolInfoProvider } from './context/SchoolInfoContext';
import ApiRetryIndicator from './components/ui/ApiRetryIndicator';
import AppRoutes from './routes/AppRoutes';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <SchoolInfoProvider>
          <ApiRetryIndicator />
          <AppRoutes />
        </SchoolInfoProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
