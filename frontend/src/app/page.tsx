import Link from 'next/link';
import { Button } from '@/components/ui/Button';

export default function HomePage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100">
      <nav className="container py-6">
        <div className="flex justify-between items-center">
          <h1 className="text-2xl font-bold text-primary-900">BeC Marketplace</h1>
          <Link href="/login">
            <Button variant="outline">Login</Button>
          </Link>
        </div>
      </nav>

      <main className="container py-20">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-5xl font-bold text-gray-900 mb-6">
            Connect. Work. Earn.
          </h2>
          <p className="text-xl text-gray-700 mb-12">
            Join the UK's premier job marketplace. Whether you're a driver looking for opportunities
            or a customer needing reliable services, we've got you covered.
          </p>

          <div className="grid md:grid-cols-2 gap-8 max-w-2xl mx-auto">
            <div className="bg-white rounded-xl p-8 shadow-lg">
              <h3 className="text-2xl font-semibold mb-4">For Drivers</h3>
              <p className="text-gray-600 mb-6">
                Register as a driver and start earning. Flexible hours, competitive pay, and instant payments.
              </p>
              <Link href="/register/driver">
                <Button fullWidth size="lg">
                  Register as Driver
                </Button>
              </Link>
            </div>

            <div className="bg-white rounded-xl p-8 shadow-lg">
              <h3 className="text-2xl font-semibold mb-4">For Customers</h3>
              <p className="text-gray-600 mb-6">
                Find reliable drivers for your needs. Vetted professionals, transparent pricing, and quality service.
              </p>
              <Link href="/register/customer">
                <Button fullWidth size="lg" variant="secondary">
                  Register as Customer
                </Button>
              </Link>
            </div>
          </div>

          <div className="mt-12">
            <p className="text-gray-600">
              Already have an account?{' '}
              <Link href="/login" className="text-primary-600 font-medium hover:underline">
                Login here
              </Link>
            </p>
          </div>
        </div>
      </main>

      <footer className="container py-8 border-t border-primary-200 mt-20">
        <p className="text-center text-gray-600">
          &copy; {new Date().getFullYear()} BeC Marketplace. All rights reserved.
        </p>
      </footer>
    </div>
  );
}
