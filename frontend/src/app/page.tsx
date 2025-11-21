import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { Card, CardContent } from '@/components/ui/Card';
import { Truck, Package, Shield, Clock, Star, CreditCard, MapPin, CheckCircle } from 'lucide-react';

export default function HomePage() {
  return (
    <div className="min-h-screen bg-white">
      {/* Navigation */}
      <nav className="bg-white shadow-sm sticky top-0 z-50">
        <div className="container mx-auto px-4 py-4">
          <div className="flex justify-between items-center">
            <div className="flex items-center space-x-2">
              <Truck className="w-8 h-8 text-primary-600" />
              <h1 className="text-2xl font-bold text-primary-900">BeC Marketplace</h1>
            </div>
            <div className="flex items-center space-x-4">
              <Link href="/book">
                <Button variant="outline">Book a Job</Button>
              </Link>
              <Link href="/login">
                <Button>Login</Button>
              </Link>
            </div>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="bg-gradient-to-br from-primary-600 to-primary-800 text-white py-20">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto text-center">
            <h2 className="text-5xl md:text-6xl font-bold mb-6">
              Your Delivery & Moving Solution
            </h2>
            <p className="text-xl md:text-2xl mb-8 text-primary-100">
              Book vetted drivers instantly. No account needed. Transparent pricing. Professional service.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Link href="/book">
                <Button size="lg" className="bg-white text-primary-600 hover:bg-primary-50">
                  <Package className="w-5 h-5 mr-2" />
                  Book a Job Now
                </Button>
              </Link>
              <Link href="/register/driver">
                <Button size="lg" variant="outline" className="border-white text-white hover:bg-white/10">
                  <Truck className="w-5 h-5 mr-2" />
                  Become a Driver
                </Button>
              </Link>
            </div>
            <p className="mt-6 text-primary-100 text-sm">
              ⚡ Instant booking • 🔒 Secure payment • ⭐ Rated 4.8/5
            </p>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-16 bg-gray-50">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h3 className="text-3xl font-bold text-gray-900 mb-4">Why Choose BeC?</h3>
            <p className="text-gray-600 max-w-2xl mx-auto">
              The UK's most trusted platform for deliveries, removals, and courier services
            </p>
          </div>

          <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
            <Card>
              <CardContent className="pt-6 text-center">
                <div className="bg-primary-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Shield className="w-8 h-8 text-primary-600" />
                </div>
                <h4 className="font-semibold text-lg mb-2">Verified Drivers</h4>
                <p className="text-gray-600 text-sm">
                  All drivers are background checked, licensed, and insured for your safety
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6 text-center">
                <div className="bg-green-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                  <CreditCard className="w-8 h-8 text-green-600" />
                </div>
                <h4 className="font-semibold text-lg mb-2">Transparent Pricing</h4>
                <p className="text-gray-600 text-sm">
                  See exact pricing upfront with no hidden fees or surprise charges
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6 text-center">
                <div className="bg-blue-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Clock className="w-8 h-8 text-blue-600" />
                </div>
                <h4 className="font-semibold text-lg mb-2">Instant Booking</h4>
                <p className="text-gray-600 text-sm">
                  Book in minutes, no account required. Get matched with drivers instantly
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6 text-center">
                <div className="bg-yellow-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Star className="w-8 h-8 text-yellow-600" />
                </div>
                <h4 className="font-semibold text-lg mb-2">Quality Service</h4>
                <p className="text-gray-600 text-sm">
                  Rated 4.8/5 stars by thousands of satisfied customers across the UK
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* How It Works */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h3 className="text-3xl font-bold text-gray-900 mb-4">How It Works</h3>
            <p className="text-gray-600">Simple, fast, and reliable - get your job done in 4 easy steps</p>
          </div>

          <div className="max-w-4xl mx-auto">
            <div className="grid md:grid-cols-4 gap-8">
              <div className="text-center">
                <div className="bg-primary-600 text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">
                  1
                </div>
                <MapPin className="w-8 h-8 text-primary-600 mx-auto mb-2" />
                <h4 className="font-semibold mb-2">Enter Details</h4>
                <p className="text-sm text-gray-600">
                  Provide pickup & delivery locations, job type, and schedule
                </p>
              </div>

              <div className="text-center">
                <div className="bg-primary-600 text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">
                  2
                </div>
                <CreditCard className="w-8 h-8 text-primary-600 mx-auto mb-2" />
                <h4 className="font-semibold mb-2">Get Quote</h4>
                <p className="text-sm text-gray-600">
                  Instant price estimate based on distance and vehicle type
                </p>
              </div>

              <div className="text-center">
                <div className="bg-primary-600 text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">
                  3
                </div>
                <Truck className="w-8 h-8 text-primary-600 mx-auto mb-2" />
                <h4 className="font-semibold mb-2">Get Matched</h4>
                <p className="text-sm text-gray-600">
                  We assign the best available driver to your job
                </p>
              </div>

              <div className="text-center">
                <div className="bg-primary-600 text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">
                  4
                </div>
                <CheckCircle className="w-8 h-8 text-primary-600 mx-auto mb-2" />
                <h4 className="font-semibold mb-2">Job Done</h4>
                <p className="text-sm text-gray-600">
                  Driver completes the job, payment processed securely
                </p>
              </div>
            </div>
          </div>

          <div className="text-center mt-12">
            <Link href="/book">
              <Button size="lg">
                Start Booking Now
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* Services Section */}
      <section className="py-16 bg-gray-50">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h3 className="text-3xl font-bold text-gray-900 mb-4">Our Services</h3>
            <p className="text-gray-600">Whatever you need moved, we've got you covered</p>
          </div>

          <div className="grid md:grid-cols-3 gap-8 max-w-5xl mx-auto">
            <Card>
              <CardContent className="pt-6">
                <Package className="w-12 h-12 text-primary-600 mb-4" />
                <h4 className="font-semibold text-lg mb-2">Deliveries</h4>
                <p className="text-gray-600 text-sm mb-4">
                  Same-day or scheduled deliveries for packages, parcels, and documents
                </p>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• E-commerce deliveries</li>
                  <li>• Document courier</li>
                  <li>• Food & groceries</li>
                </ul>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <Truck className="w-12 h-12 text-primary-600 mb-4" />
                <h4 className="font-semibold text-lg mb-2">Removals</h4>
                <p className="text-gray-600 text-sm mb-4">
                  House moves, office relocations, and furniture transport
                </p>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• Home removals</li>
                  <li>• Office moves</li>
                  <li>• Furniture delivery</li>
                </ul>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <MapPin className="w-12 h-12 text-primary-600 mb-4" />
                <h4 className="font-semibold text-lg mb-2">Errands</h4>
                <p className="text-gray-600 text-sm mb-4">
                  Pick up and drop off services for any task you need done
                </p>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• Shopping runs</li>
                  <li>• Prescription pickup</li>
                  <li>• Custom errands</li>
                </ul>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* CTA Sections */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="grid md:grid-cols-2 gap-8 max-w-5xl mx-auto">
            <Card variant="elevated" className="bg-gradient-to-br from-primary-50 to-primary-100">
              <CardContent className="p-8">
                <h3 className="text-2xl font-bold mb-4">Need Something Moved?</h3>
                <p className="text-gray-700 mb-6">
                  Book instantly without creating an account. Get matched with verified drivers and track your delivery in real-time.
                </p>
                <Link href="/book">
                  <Button size="lg">
                    <Package className="w-5 h-5 mr-2" />
                    Book a Job
                  </Button>
                </Link>
                <p className="text-sm text-gray-600 mt-4">
                  Or{' '}
                  <Link href="/register/customer" className="text-primary-600 hover:underline font-medium">
                    create an account
                  </Link>{' '}
                  to manage multiple jobs and save your preferences
                </p>
              </CardContent>
            </Card>

            <Card variant="elevated" className="bg-gradient-to-br from-green-50 to-green-100">
              <CardContent className="p-8">
                <h3 className="text-2xl font-bold mb-4">Earn as a Driver</h3>
                <p className="text-gray-700 mb-6">
                  Join our network of professional drivers. Flexible schedule, competitive earnings, and weekly payouts.
                </p>
                <Link href="/register/driver">
                  <Button size="lg" variant="success">
                    <Truck className="w-5 h-5 mr-2" />
                    Become a Driver
                  </Button>
                </Link>
                <p className="text-sm text-gray-600 mt-4">
                  ⭐ 4.8/5 driver satisfaction • 💷 Earn £500-£2000/week
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-white py-12">
        <div className="container mx-auto px-4">
          <div className="grid md:grid-cols-4 gap-8">
            <div>
              <div className="flex items-center space-x-2 mb-4">
                <Truck className="w-6 h-6" />
                <span className="font-bold text-lg">BeC Marketplace</span>
              </div>
              <p className="text-gray-400 text-sm">
                The UK's trusted platform for deliveries, removals, and courier services.
              </p>
            </div>

            <div>
              <h4 className="font-semibold mb-4">For Customers</h4>
              <ul className="space-y-2 text-sm text-gray-400">
                <li><Link href="/book" className="hover:text-white">Book a Job</Link></li>
                <li><Link href="/register/customer" className="hover:text-white">Create Account</Link></li>
                <li><Link href="/login" className="hover:text-white">Login</Link></li>
              </ul>
            </div>

            <div>
              <h4 className="font-semibold mb-4">For Drivers</h4>
              <ul className="space-y-2 text-sm text-gray-400">
                <li><Link href="/register/driver" className="hover:text-white">Become a Driver</Link></li>
                <li><Link href="/login" className="hover:text-white">Driver Login</Link></li>
              </ul>
            </div>

            <div>
              <h4 className="font-semibold mb-4">Company</h4>
              <ul className="space-y-2 text-sm text-gray-400">
                <li><a href="#" className="hover:text-white">About Us</a></li>
                <li><a href="#" className="hover:text-white">Contact</a></li>
                <li><a href="#" className="hover:text-white">Privacy Policy</a></li>
                <li><a href="#" className="hover:text-white">Terms of Service</a></li>
              </ul>
            </div>
          </div>

          <div className="border-t border-gray-800 mt-8 pt-8 text-center text-gray-400 text-sm">
            <p>&copy; {new Date().getFullYear()} BeC Marketplace. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
