import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-slate-50 via-primary-50 to-primary-50">
      <!-- Navigation -->
      <nav class="fixed top-0 w-full bg-white/80 backdrop-blur-md border-b border-gray-200 z-50">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="flex justify-between items-center h-16">
            <div class="flex items-center space-x-2">
              <img src="/logo.png" alt="LoadLink Logo" class="h-12 w-auto" />
              <span class="text-2xl font-bold text-gray-900">
                LoadLink
              </span>
            </div>

            <div class="hidden md:flex items-center space-x-8">
              <a href="#features" class="text-gray-600 hover:text-primary-600 transition-colors">Features</a>
              <a href="#how-it-works" class="text-gray-600 hover:text-primary-600 transition-colors">How It Works</a>
              <a href="#pricing" class="text-gray-600 hover:text-primary-600 transition-colors">Pricing</a>
              <button
                (click)="navigateToLogin()"
                class="px-4 py-2 text-primary-600 hover:text-primary-700 font-medium transition-colors"
              >
                Sign In
              </button>
              <button
                (click)="navigateToBooking()"
                class="px-6 py-2.5 bg-gradient-to-r from-primary-600 to-primary-700 hover:from-primary-700 hover:to-primary-800 text-white rounded-lg font-medium shadow-lg shadow-primary-500/30 transition-all"
              >
                Book Now
              </button>
            </div>

            <!-- Mobile menu button -->
            <button class="md:hidden p-2 rounded-lg hover:bg-gray-100">
              <svg class="w-6 h-6 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
              </svg>
            </button>
          </div>
        </div>
      </nav>

      <!-- Hero Section -->
      <section class="pt-32 pb-20 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <div class="grid lg:grid-cols-2 gap-12 items-center">
            <div class="space-y-8">
              <div class="inline-block">
                <span class="px-4 py-2 bg-primary-100 text-primary-700 rounded-full text-sm font-semibold">
                  🚀 Fast, Reliable, Professional
                </span>
              </div>

              <h1 class="text-5xl lg:text-6xl font-bold text-gray-900 leading-tight">
                Your Delivery,
                <span class="bg-gradient-to-r from-primary-600 to-primary-700 bg-clip-text text-transparent">
                  Our Priority
                </span>
              </h1>

              <p class="text-xl text-gray-600 leading-relaxed">
                Experience seamless logistics with LoadLink. From same-day deliveries to scheduled routes,
                we connect you with trusted drivers for all your transportation needs.
              </p>

              <div class="flex flex-col sm:flex-row gap-4">
                <button
                  (click)="navigateToRegisterCustomer()"
                  class="px-8 py-4 bg-gradient-to-r from-primary-600 to-primary-700 hover:from-primary-700 hover:to-primary-800 text-white rounded-xl font-semibold shadow-xl shadow-primary-500/30 transition-all transform hover:scale-105"
                >
                  Register as Customer
                </button>
                <button
                  (click)="navigateToRegisterDriver()"
                  class="px-8 py-4 bg-white hover:bg-primary-50 text-primary-700 rounded-xl font-semibold border-2 border-primary-600 transition-all transform hover:scale-105"
                >
                  Become a Driver
                </button>
              </div>

              <div class="flex items-center gap-8 pt-4">
                <div class="flex items-center gap-2">
                  <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                  </svg>
                  <span class="text-gray-700 font-medium">4.6/5 Rating</span>
                </div>
                <div class="text-gray-700 font-medium">250+ Deliveries</div>
                <div class="text-gray-700 font-medium">15+ Drivers</div>
              </div>
            </div>

            <div class="relative">
              <div class="absolute inset-0 bg-gradient-to-r from-primary-500 to-primary-600 rounded-3xl opacity-10 blur-3xl"></div>
              <div class="relative bg-white rounded-3xl shadow-2xl p-8 border border-gray-100">
                <div class="space-y-6">
                  <!-- Quick Booking Card -->
                  <div class="bg-gradient-to-br from-primary-50 to-primary-100 rounded-2xl p-6 border border-primary-100">
                    <div class="flex items-center justify-between mb-4">
                      <span class="text-sm font-semibold text-primary-700">Quick Booking</span>
                      <span class="px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-medium">Available Now</span>
                    </div>
                    <div class="space-y-3">
                      <div class="flex items-center gap-3">
                        <div class="w-10 h-10 bg-primary-600 rounded-lg flex items-center justify-center">
                          <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
                          </svg>
                        </div>
                        <div class="flex-1">
                          <p class="text-xs text-gray-500">Pickup</p>
                          <p class="font-medium text-gray-900">123 Main Street</p>
                        </div>
                      </div>
                      <div class="flex items-center gap-3">
                        <div class="w-10 h-10 bg-primary-700 rounded-lg flex items-center justify-center">
                          <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                          </svg>
                        </div>
                        <div class="flex-1">
                          <p class="text-xs text-gray-500">Delivery</p>
                          <p class="font-medium text-gray-900">456 Oak Avenue</p>
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- Driver Info -->
                  <div class="flex items-center gap-4 p-4 bg-gray-50 rounded-xl">
                    <div class="w-12 h-12 bg-gradient-to-br from-primary-600 to-primary-700 rounded-full"></div>
                    <div class="flex-1">
                      <p class="font-semibold text-gray-900">Professional Driver</p>
                      <div class="flex items-center gap-1">
                        <svg class="w-4 h-4 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                        </svg>
                        <span class="text-sm text-gray-600">4.7 • 45 deliveries</span>
                      </div>
                    </div>
                    <span class="px-3 py-1 bg-primary-100 text-primary-700 rounded-lg text-sm font-medium">10 min</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Features Section -->
      <section id="features" class="py-20 bg-white">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="text-center mb-16">
            <span class="px-4 py-2 bg-primary-100 text-primary-700 rounded-full text-sm font-semibold">Features</span>
            <h2 class="mt-4 text-4xl font-bold text-gray-900">Everything You Need</h2>
            <p class="mt-4 text-xl text-gray-600">Powerful features to make your deliveries seamless</p>
          </div>

          <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            <div class="group p-8 bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-200 hover:border-primary-300 hover:shadow-xl transition-all">
              <div class="w-14 h-14 bg-gradient-to-br from-primary-600 to-primary-700 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                </svg>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-3">Real-Time Tracking</h3>
              <p class="text-gray-600">Track your deliveries in real-time with live GPS updates and ETA notifications.</p>
            </div>

            <div class="group p-8 bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-200 hover:border-indigo-300 hover:shadow-xl transition-all">
              <div class="w-14 h-14 bg-gradient-to-br from-blue-600 to-indigo-500 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-3">Same-Day Delivery</h3>
              <p class="text-gray-600">Get your packages delivered the same day with our express delivery service.</p>
            </div>

            <div class="group p-8 bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-200 hover:border-primary-300 hover:shadow-xl transition-all">
              <div class="w-14 h-14 bg-gradient-to-br from-primary-600 to-primary-700 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                </svg>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-3">Secure & Insured</h3>
              <p class="text-gray-600">Your packages are protected with full insurance coverage and secure handling.</p>
            </div>

            <div class="group p-8 bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-200 hover:border-primary-300 hover:shadow-xl transition-all">
              <div class="w-14 h-14 bg-gradient-to-br from-primary-700 to-primary-800 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-3">Transparent Pricing</h3>
              <p class="text-gray-600">No hidden fees. Get instant quotes and pay only for what you need.</p>
            </div>

            <div class="group p-8 bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-200 hover:border-primary-300 hover:shadow-xl transition-all">
              <div class="w-14 h-14 bg-gradient-to-br from-primary-600 to-primary-700 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/>
                </svg>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-3">Verified Drivers</h3>
              <p class="text-gray-600">All drivers are background-checked and professionally trained.</p>
            </div>

            <div class="group p-8 bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-200 hover:border-primary-300 hover:shadow-xl transition-all">
              <div class="w-14 h-14 bg-gradient-to-br from-primary-700 to-primary-800 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 5.636l-3.536 3.536m0 5.656l3.536 3.536M9.172 9.172L5.636 5.636m3.536 9.192l-3.536 3.536M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-5 0a4 4 0 11-8 0 4 4 0 018 0z"/>
                </svg>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-3">24/7 Support</h3>
              <p class="text-gray-600">Our customer support team is always available to help you.</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Stats Section -->
      <section class="py-16 bg-white">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="grid grid-cols-2 md:grid-cols-4 gap-8">
            <div class="text-center">
              <div class="text-4xl md:text-5xl font-bold text-primary-600 mb-2">100+</div>
              <div class="text-gray-600 font-medium">Happy Customers</div>
            </div>
            <div class="text-center">
              <div class="text-4xl md:text-5xl font-bold text-primary-600 mb-2">15+</div>
              <div class="text-gray-600 font-medium">Professional Drivers</div>
            </div>
            <div class="text-center">
              <div class="text-4xl md:text-5xl font-bold text-primary-600 mb-2">95%</div>
              <div class="text-gray-600 font-medium">On-Time Delivery</div>
            </div>
            <div class="text-center">
              <div class="text-4xl md:text-5xl font-bold text-primary-600 mb-2">24/7</div>
              <div class="text-gray-600 font-medium">Customer Support</div>
            </div>
          </div>
        </div>
      </section>

      <!-- How It Works -->
      <section id="how-it-works" class="py-20 bg-gradient-to-br from-primary-50 to-primary-100">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="text-center mb-16">
            <span class="px-4 py-2 bg-white text-primary-700 rounded-full text-sm font-semibold shadow-sm">How It Works</span>
            <h2 class="mt-4 text-4xl font-bold text-gray-900">Simple & Fast Process</h2>
            <p class="mt-4 text-xl text-gray-600">Get your delivery done in 3 easy steps</p>
          </div>

          <div class="grid md:grid-cols-3 gap-8">
            <div class="relative">
              <div class="text-center">
                <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-primary-600 to-primary-700 text-white rounded-2xl text-3xl font-bold mb-6 shadow-xl shadow-primary-500/30">
                  1
                </div>
                <h3 class="text-2xl font-bold text-gray-900 mb-4">Book Your Delivery</h3>
                <p class="text-gray-600">Enter pickup and delivery details, choose your vehicle type, and get an instant quote.</p>
              </div>
              <div class="hidden md:block absolute top-10 left-full w-full h-0.5 bg-gradient-to-r from-primary-600 to-primary-700 opacity-20 -ml-4"></div>
            </div>

            <div class="relative">
              <div class="text-center">
                <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-primary-700 to-primary-800 text-white rounded-2xl text-3xl font-bold mb-6 shadow-xl shadow-primary-500/30">
                  2
                </div>
                <h3 class="text-2xl font-bold text-gray-900 mb-4">Get Matched</h3>
                <p class="text-gray-600">Our system instantly matches you with the best available driver in your area.</p>
              </div>
              <div class="hidden md:block absolute top-10 left-full w-full h-0.5 bg-gradient-to-r from-primary-700 to-primary-800 opacity-20 -ml-4"></div>
            </div>

            <div class="text-center">
              <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-primary-600 to-primary-700 text-white rounded-2xl text-3xl font-bold mb-6 shadow-xl shadow-primary-500/30">
                3
              </div>
              <h3 class="text-2xl font-bold text-gray-900 mb-4">Track & Receive</h3>
              <p class="text-gray-600">Track your delivery in real-time and receive updates every step of the way.</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Testimonials Section -->
      <section class="py-20 bg-white">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="text-center mb-16">
            <span class="px-4 py-2 bg-primary-100 text-primary-700 rounded-full text-sm font-semibold">Testimonials</span>
            <h2 class="mt-4 text-4xl font-bold text-gray-900">What Our Customers Say</h2>
            <p class="mt-4 text-xl text-gray-600">Hear from our growing community of users</p>
          </div>

          <div class="grid md:grid-cols-3 gap-8">
            <div class="bg-gray-50 rounded-2xl p-8 border border-gray-200">
              <div class="flex items-center gap-1 mb-4">
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
              </div>
              <p class="text-gray-700 mb-6">"LoadLink has been helpful for our small business deliveries. The service is reliable and easy to use."</p>
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 bg-gradient-to-br from-primary-600 to-primary-700 rounded-full flex items-center justify-center text-white font-bold">
                  SM
                </div>
                <div>
                  <div class="font-semibold text-gray-900">Sarah Mitchell</div>
                  <div class="text-sm text-gray-500">E-commerce Owner</div>
                </div>
              </div>
            </div>

            <div class="bg-gray-50 rounded-2xl p-8 border border-gray-200">
              <div class="flex items-center gap-1 mb-4">
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
              </div>
              <p class="text-gray-700 mb-6">"The real-time tracking feature is convenient. I can keep an eye on where my packages are and the service has been good so far."</p>
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 bg-gradient-to-br from-primary-600 to-primary-700 rounded-full flex items-center justify-center text-white font-bold">
                  JD
                </div>
                <div>
                  <div class="font-semibold text-gray-900">James Davidson</div>
                  <div class="text-sm text-gray-500">Small Business Owner</div>
                </div>
              </div>
            </div>

            <div class="bg-gray-50 rounded-2xl p-8 border border-gray-200">
              <div class="flex items-center gap-1 mb-4">
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
                <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
              </div>
              <p class="text-gray-700 mb-6">"As a new driver on LoadLink, I'm getting regular work opportunities and the platform is straightforward to use. Payments come through reliably."</p>
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 bg-gradient-to-br from-primary-600 to-primary-700 rounded-full flex items-center justify-center text-white font-bold">
                  MC
                </div>
                <div>
                  <div class="font-semibold text-gray-900">Maria Chen</div>
                  <div class="text-sm text-gray-500">Professional Driver</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- CTA Section -->
      <section class="py-20 bg-gradient-to-r from-primary-600 to-primary-700">
        <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="text-center mb-10">
            <h2 class="text-4xl font-bold text-white mb-4">Ready to Get Started?</h2>
            <p class="text-xl text-primary-100">Join our growing community today</p>
          </div>

          <div class="grid md:grid-cols-2 gap-6 mb-8">
            <!-- Customer CTA -->
            <div class="bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-8 text-white hover:bg-white/15 transition-all">
              <div class="text-center">
                <svg class="w-16 h-16 mx-auto mb-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                </svg>
                <h3 class="text-2xl font-bold mb-2">Need Delivery?</h3>
                <p class="text-primary-100 mb-6">Register as a customer and book your first delivery in minutes</p>
                <button
                  (click)="navigateToRegisterCustomer()"
                  class="w-full px-6 py-3 bg-white hover:bg-gray-100 text-primary-600 rounded-xl font-semibold shadow-xl transition-all transform hover:scale-105"
                >
                  Register as Customer
                </button>
              </div>
            </div>

            <!-- Driver CTA -->
            <div class="bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-8 text-white hover:bg-white/15 transition-all">
              <div class="text-center">
                <svg class="w-16 h-16 mx-auto mb-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                </svg>
                <h3 class="text-2xl font-bold mb-2">Want to Earn?</h3>
                <p class="text-primary-100 mb-6">Join as a driver and start earning with your vehicle today</p>
                <button
                  (click)="navigateToRegisterDriver()"
                  class="w-full px-6 py-3 bg-white hover:bg-gray-100 text-primary-600 rounded-xl font-semibold shadow-xl transition-all transform hover:scale-105"
                >
                  Become a Driver
                </button>
              </div>
            </div>
          </div>

          <div class="text-center">
            <p class="text-primary-100 mb-4">Already have an account?</p>
            <button
              (click)="navigateToLogin()"
              class="px-8 py-3 bg-primary-800 hover:bg-primary-900 text-white rounded-xl font-semibold border-2 border-white/20 transition-all inline-flex items-center"
            >
              Sign In to Dashboard
            </button>
          </div>
        </div>
      </section>

      <!-- Footer -->
      <footer class="bg-gray-900 text-gray-300 py-12">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="grid md:grid-cols-4 gap-8">
            <div>
              <div class="flex items-center space-x-2 mb-4">
                <img src="/logo.png" alt="LoadLink Logo" class="h-10 w-auto" />
                <span class="text-xl font-bold text-white">LoadLink</span>
              </div>
              <p class="text-gray-400">Your trusted delivery partner for fast, reliable logistics.</p>
            </div>

            <div>
              <h4 class="text-white font-semibold mb-4">Company</h4>
              <ul class="space-y-2">
                <li><a href="#" class="hover:text-white transition-colors">About Us</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Careers</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Press</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Blog</a></li>
              </ul>
            </div>

            <div>
              <h4 class="text-white font-semibold mb-4">Support</h4>
              <ul class="space-y-2">
                <li><a href="#" class="hover:text-white transition-colors">Help Center</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Safety</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Terms of Service</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Privacy Policy</a></li>
              </ul>
            </div>

            <div>
              <h4 class="text-white font-semibold mb-4">Connect</h4>
              <ul class="space-y-2">
                <li><a href="#" class="hover:text-white transition-colors">Twitter</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Facebook</a></li>
                <li><a href="#" class="hover:text-white transition-colors">LinkedIn</a></li>
                <li><a href="#" class="hover:text-white transition-colors">Instagram</a></li>
              </ul>
            </div>
          </div>

          <div class="mt-12 pt-8 border-t border-gray-800 text-center text-gray-400">
            <p>&copy; 2024 LoadLink. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class LandingComponent {
  constructor(private router: Router) {}

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  navigateToBooking(): void {
    this.router.navigate(['/book']);
  }

  navigateToRegisterCustomer(): void {
    this.router.navigate(['/register-customer']);
  }

  navigateToRegisterDriver(): void {
    this.router.navigate(['/register-driver']);
  }
}
