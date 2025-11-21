import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';

export default function UnauthorizedPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center p-4">
      <div className="max-w-md w-full">
        <Alert variant="error" className="mb-6">
          <h2 className="text-lg font-semibold mb-2">Access Denied</h2>
          <p>You don't have permission to access this page. Please contact your administrator if you believe this is an error.</p>
        </Alert>

        <div className="text-center">
          <Link href="/">
            <Button>Return to Home</Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
