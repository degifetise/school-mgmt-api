import { useEffect, useRef } from 'react';
import { LogOut, X } from 'lucide-react';
import { Alert, Spinner } from '../ui/Feedback';

export default function LogoutConfirmationModal({
  isOpen,
  isLoggingOut,
  onCancel,
  onConfirm,
}) {
  const cancelButtonRef = useRef(null);

  useEffect(() => {
    if (!isOpen) return undefined;
    cancelButtonRef.current?.focus();
    function closeOnEscape(event) {
      if (event.key === 'Escape' && !isLoggingOut) onCancel();
    }

    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [isOpen, isLoggingOut, onCancel]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[100] grid place-items-center bg-slate-950/60 p-4">
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="logout-confirmation-title"
        className="card w-full max-w-sm p-6 shadow-xl"
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 id="logout-confirmation-title" className="text-lg font-bold text-slate-900">
              Confirm logout
            </h2>
            <p className="mt-2 text-sm text-slate-600">Are you sure you want to log out?</p>
          </div>
          <button
            ref={cancelButtonRef}
            type="button"
            className="btn-secondary"
            aria-label="Cancel logout"
            onClick={onCancel}
            disabled={isLoggingOut}
          >
            <X className="size-4" aria-hidden="true" />
          </button>
        </div>

        {isLoggingOut && (
          <Alert variant="info" className="mt-4">
            Signing you out…
          </Alert>
        )}

        <div className="mt-6 flex justify-end gap-2">
          <button
            type="button"
            className="btn-secondary"
            onClick={onCancel}
            disabled={isLoggingOut}
          >
            Cancel
          </button>
          <button
            type="button"
            className="btn-primary"
            onClick={onConfirm}
            disabled={isLoggingOut}
          >
            {isLoggingOut ? <Spinner className="size-4" /> : <LogOut className="size-4" aria-hidden="true" />}
            Confirm Logout
          </button>
        </div>
      </section>
    </div>
  );
}
