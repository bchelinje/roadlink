# BeC OpenID Connect - Frontend

A modern Next.js frontend for the BeC Job Marketplace platform, featuring self-signup with driver approval workflow.

## Features

- **Self-Signup**: Drivers and customers can register themselves
- **Driver Approval Workflow**: Admin dashboard to review and approve/reject driver applications
- **Role-Based Authentication**: Secure login with JWT tokens and role-based access control
- **Responsive Design**: Mobile-friendly UI built with Tailwind CSS
- **Form Validation**: Client-side validation using Zod and React Hook Form
- **Type Safety**: Full TypeScript support

## Tech Stack

- **Framework**: Next.js 14 (App Router)
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **Forms**: React Hook Form + Zod validation
- **HTTP Client**: Axios
- **Icons**: Lucide React
- **Date Handling**: date-fns

## Getting Started

### Prerequisites

- Node.js 18+ and npm/yarn
- Backend API running (default: http://localhost:5000)

### Installation

1. Navigate to the frontend directory:
```bash
cd frontend
```

2. Install dependencies:
```bash
npm install
# or
yarn install
```

3. Create environment file:
```bash
cp .env.example .env
```

4. Update the `.env` file with your backend API URL:
```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Running the Development Server

```bash
npm run dev
# or
yarn dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Building for Production

```bash
npm run build
npm start
# or
yarn build
yarn start
```

## Project Structure

```
frontend/
├── src/
│   ├── app/                    # Next.js App Router pages
│   │   ├── admin/
│   │   │   └── dashboard/      # Admin approval dashboard
│   │   ├── register/
│   │   │   ├── driver/         # Driver signup form
│   │   │   └── customer/       # Customer signup form
│   │   ├── login/              # Login page
│   │   ├── layout.tsx          # Root layout
│   │   ├── page.tsx            # Landing page
│   │   └── globals.css         # Global styles
│   ├── components/             # React components
│   │   ├── ui/                 # Reusable UI components
│   │   │   ├── Button.tsx
│   │   │   ├── Input.tsx
│   │   │   ├── Select.tsx
│   │   │   ├── Card.tsx
│   │   │   └── Alert.tsx
│   │   └── ProtectedRoute.tsx  # Auth guard component
│   ├── contexts/               # React contexts
│   │   └── AuthContext.tsx     # Authentication state management
│   ├── services/               # API services
│   │   └── api.service.ts      # Backend API calls
│   ├── lib/                    # Utility libraries
│   │   └── api-client.ts       # Axios instance with interceptors
│   └── types/                  # TypeScript type definitions
│       └── index.ts            # All TypeScript interfaces
├── public/                     # Static assets
├── package.json
├── tsconfig.json
├── tailwind.config.js
├── next.config.js
└── README.md
```

## Key Features Implemented

### 1. User Registration

#### Driver Registration (`/register/driver`)
Comprehensive multi-section form including:
- Account credentials
- Personal information
- Address details
- Driving license information
- Vehicle details
- Bank account information
- Emergency contact

#### Customer Registration (`/register/customer`)
Simplified form including:
- Account credentials
- Personal information
- Address details
- Optional company name

### 2. Authentication (`/login`)
- Email/password login
- JWT token management
- Role-based redirects after login
- Automatic token refresh
- Secure token storage in localStorage

### 3. Admin Dashboard (`/admin/dashboard`)
Protected route for Admin and SuperAdmin roles featuring:
- List of pending driver applications
- Detailed driver information viewer
- One-click approve/reject actions
- Real-time status updates
- Comprehensive driver details modal

### 4. Protected Routes
Role-based access control:
- Admin pages require Admin or SuperAdmin role
- Automatic redirect to login if not authenticated
- Automatic redirect to unauthorized page if insufficient permissions

## API Integration

All backend endpoints are integrated via `ApiService`:

### Registration
- `POST /api/registration/driver` - Driver signup
- `POST /api/registration/customer` - Customer signup

### Authentication
- `POST /connect/token` - Login with email/password
- `POST /connect/token` - Refresh token
- `POST /connect/logout` - Logout

### Vetting/Approval
- `GET /api/vetting/pending` - Get pending drivers
- `GET /api/vetting/driver/:id` - Get driver details
- `POST /api/vetting/approve/:id` - Approve driver
- `POST /api/vetting/reject/:id` - Reject driver
- `POST /api/vetting/suspend/:id` - Suspend driver
- `POST /api/vetting/bulk-approve` - Bulk approve drivers
- `POST /api/vetting/bulk-reject` - Bulk reject drivers

### Driver Management
- `GET /api/drivers` - Get all drivers
- `GET /api/drivers/approved` - Get approved drivers
- `GET /api/drivers/rejected` - Get rejected drivers
- `GET /api/drivers/suspended` - Get suspended drivers

### Customer Management
- `GET /api/customers` - Get all customers
- `GET /api/customers/:id` - Get customer by ID

### User Profile
- `GET /api/users/me` - Get current user profile
- `PUT /api/users/me` - Update profile

## User Roles

The system supports 4 user roles:
1. **SuperAdmin** - Full system access
2. **Admin** - Can approve/reject drivers, manage users
3. **Driver** - Can accept jobs, view earnings
4. **Customer** - Can post jobs, hire drivers

## Approval Status Flow

```
Registration → Pending → Approved/Rejected/Suspended
```

- **Pending**: Initial state after driver registration
- **Approved**: Driver can access platform and accept jobs
- **Rejected**: Application denied (with reason)
- **Suspended**: Previously approved driver temporarily blocked

## Form Validation

All forms include comprehensive validation:
- Email format validation
- Password strength requirements (min 8 characters)
- UK phone number format validation
- UK postcode validation
- National Insurance number format
- Driving license validation
- Bank account format (sort code, account number)
- Vehicle registration number
- Required field validation
- Password confirmation matching

## Styling

The application uses a custom design system built on Tailwind CSS:
- **Primary Color**: Blue (customizable in `tailwind.config.js`)
- **Responsive**: Mobile-first design
- **Components**: Reusable UI components with consistent styling
- **Accessibility**: ARIA labels and keyboard navigation support

## Error Handling

Comprehensive error handling throughout:
- API error messages displayed to users
- Network error detection
- 401 unauthorized automatic redirect
- Form validation errors
- Loading states for async operations

## Security Features

- JWT token authentication
- Automatic token expiration handling
- Secure token storage
- Protected routes with role-based access
- HTTPS upgrade for all HTTP URLs
- XSS protection via React's built-in escaping
- CSRF protection via token-based auth

## Future Enhancements

Potential additions:
- Email verification flow
- Password reset functionality
- Document upload UI
- Driver dashboard with job listings
- Customer dashboard with job posting
- Real-time notifications
- Chat/messaging system
- Rating and review system
- Payment integration UI
- Advanced filtering and search
- Export functionality
- Analytics dashboard

## Development

### Type Checking
```bash
npm run type-check
```

### Linting
```bash
npm run lint
```

### Code Organization
- Keep components small and focused
- Use TypeScript for type safety
- Follow Next.js App Router conventions
- Maintain consistent naming conventions
- Document complex logic with comments

## Troubleshooting

### API Connection Issues
- Ensure backend is running on the correct port
- Check `NEXT_PUBLIC_API_URL` in `.env`
- Verify CORS is enabled on backend

### Authentication Issues
- Clear localStorage and try logging in again
- Check JWT token expiration
- Verify user roles in backend

### Build Errors
- Delete `.next` folder and rebuild
- Clear node_modules and reinstall dependencies
- Check TypeScript errors with `npm run type-check`

## Support

For issues or questions:
1. Check the backend API documentation
2. Review the TypeScript types in `src/types/index.ts`
3. Check browser console for error messages

## License

This project is part of the BeC OpenID Connect platform.
