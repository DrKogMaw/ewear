# Ewear

An Angular + ASP.NET Core storefront for shirts, pants, and boots.

## What is included

- Responsive product collection with category filters, non-interrupting bag feedback, a persistent shopping bag, and a saved dark-mode preference
- Phone based registration and sign-in with a six-digit verification code
- SQLite catalog and account storage
- Protected staff product creation, photo upload, editing, and removal
- Twilio SMS delivery for configured production environments

Checkout and payment processing are outside this initial scope. Prices and seed products are illustrative.

## Run locally

Install Node.js 24.15+ and .NET SDK 10. Then run in separate terminals:

```bash
cd backend
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5276
```

```bash
cd frontend
npm install
npm start
```

Open http://localhost:4200. In Development, requesting a verification code displays it in the sign-in dialog. The Angular dev server proxies `/api` to ASP.NET Core.

## Production configuration

Set these environment variables on the backend host:

```text
Auth__SigningKey=<long random secret>
Sms__AccountSid=<Twilio account SID>
Sms__AuthToken=<Twilio auth token>
Sms__FromNumber=<Twilio sender number>
Staff__PhoneNumbers=+15551234567,+15557654321
Frontend__Origin=https://your-store-domain.example
ConnectionStrings__Store=Data Source=/path/to/persistent/ewear.db
```

Only a verified phone number listed in `Staff__PhoneNumbers` receives the `staff` role. The server checks that role for every product mutation. Staff access is an explicit authorization path, not a hidden bypass. Protect the staff phone numbers and the signing key as production secrets.

Uploaded product photos are stored in `backend/uploads` by default. Put that directory on persistent storage alongside the SQLite database in production.

Build the frontend with `npm run build`, serve its `dist/frontend/browser` directory from a static web host, and route `/api` requests to the backend. Configure HTTPS on both hosts. The backend refuses production startup without `Auth__SigningKey` and refuses SMS requests without Twilio configuration.

The initial SQLite schema is created with `EnsureCreated` for this starter. Use EF Core migrations before evolving the production schema.
