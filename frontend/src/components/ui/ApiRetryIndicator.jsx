import { useSyncExternalStore } from 'react';
import { Loader2 } from 'lucide-react';
import { getApiRetryState, subscribeApiRetryState } from '../../services/api';

export default function ApiRetryIndicator() {
  const retryState = useSyncExternalStore(subscribeApiRetryState, getApiRetryState);
  if (retryState.activeRequests === 0) return null;

  return (
    <div
      className="fixed inset-x-0 top-0 z-[100] flex justify-center px-4 pt-3"
      role="status"
      aria-live="polite"
    >
      <div className="inline-flex items-center gap-2 rounded-full border border-brand-200 bg-white px-4 py-2 text-sm font-medium text-brand-800 shadow-lg">
        <Loader2 className="size-4 animate-spin" aria-hidden="true" />
        connecting to the server… retry {retryState.attempt} of 3
      </div>
    </div>
  );
}
