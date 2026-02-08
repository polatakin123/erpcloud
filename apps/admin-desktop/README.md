# ERP Cloud Admin Desktop

Desktop administration console for ERP Cloud - built with Tauri v2, React, and TypeScript.

**🇹🇷 Kullanıcı Arayüzü:** Uygulama arayüzü **tamamen Türkçedir**. Yedek parça bayileri, muhasebe ve depo personeli için optimize edilmiştir.

**Terminoloji:**
- Product → Ürün
- Stock Card → Stok Kartı (daily workflow)
- Warehouse → Depo
- Customer → Müşteri
- Supplier → Tedarikçi
- Party → Cari
- Sales Order → Satış Siparişi
- Invoice → Fatura
- Shipment → Sevkiyat

## Prerequisites

- **Node.js** (v18 or higher)
- **npm** or **pnpm**
- **Rust** (latest stable)
  - Install via: https://rustup.rs/
- **ERP Cloud Backend** running locally

## Installation

```bash
# Navigate to the project directory
cd apps/admin-desktop

# Install dependencies
npm install
```

## Running the Application

### 1. Start the Backend

First, ensure the ERP Cloud backend is running:

```bash
cd ../../src/Api
dotnet run
```

The backend should be available at `http://localhost:5000` (or your configured port).

### 2. Start the Desktop App

```bash
# In apps/admin-desktop directory
npm run tauri dev
```

This will:
- Start the Vite dev server on port 1420
- Build and launch the Tauri desktop application

## Configuration

### API Base URL

1. Click **Settings** in the sidebar
2. Enter your API base URL (default: `http://localhost:5000`)
3. Click **Save Settings**

The URL is persisted locally and will be remembered across app restarts.

Alternatively, set the environment variable before building:

```bash
# .env.local
VITE_API_BASE_URL=http://localhost:5000
```

## Authentication

### Getting a JWT Token

1. Use Keycloak or your authentication system to obtain a JWT token
2. For development, you can use the backend's debug endpoint or login via Postman
3. Copy the entire JWT token (it starts with `eyJ...`)

### Logging In

1. Launch the desktop app
2. Paste your JWT token into the "JWT Token" field
3. Click **Login**

The token is securely stored using Tauri's store plugin and will persist across sessions.

### Logging Out

Click the **Logout** button in the sidebar footer. This clears your stored token.

## Demo Workflow

Here's a complete end-to-end demo flow to test the application:

### 1. Create a Party

1. Navigate to **Parties** in the sidebar
2. Click **New Party**
3. Fill in:
   - Code: `CUST001`
   - Name: `Demo Customer`
   - Type: `CUSTOMER`
4. Click **Save**

### 2. Create Product and Variant

1. Navigate to **Products**
2. Click **New Product**
3. Fill in:
   - Code: `PROD001`
   - Name: `Demo Product`
4. Click **Save**
5. Click **Add Variant**
6. Fill in:
   - SKU: `PROD001-STD`
   - Name: `Standard`
7. Click **Save**

### 3. Create Sales Order

1. Navigate to **Sales Orders**
2. Click **New Order**
3. Select:
   - Party: `Demo Customer`
   - Branch: (select your branch)
4. Add line item:
   - Variant: `PROD001-STD`
   - Quantity: `10`
   - Unit Price: `100`
5. Click **Save Draft**
6. Click **Confirm Order**

### 4. Create Shipment

1. Navigate to **Shipments**
2. Click **New Shipment**
3. Select the sales order created above
4. Enter:
   - Warehouse: (select warehouse)
   - Shipment No: `SHP001`
5. Lines will auto-populate from the order
6. Click **Save Draft**
7. Click **Ship**

### 5. Create Invoice from Shipment

1. Navigate to **Invoices**
2. Click **Create from Shipment**
3. Select the shipment created above
4. Review the invoice lines (auto-populated)
5. Enter Invoice No: `INV001`
6. Click **Save Draft**
7. Click **Issue Invoice**

### 6. Create Cashbox

1. Navigate to **Cashboxes**
2. Click **New Cashbox**
3. Fill in:
   - Code: `CASH001`
   - Name: `Main Cashbox`
   - Currency: `TRY`
   - Is Default: `true`
4. Click **Save**

### 7. Record Payment

1. Navigate to **Payments**
2. Click **New Payment**
3. Fill in:
   - Payment No: `PAY001`
   - Party: `Demo Customer`
   - Branch: (select branch)
   - Direction: `IN` (incoming payment)
   - Method: `CASH`
   - Currency: `TRY`
   - Amount: `500`
   - Source Type: `CASHBOX`
   - Source: `Main Cashbox`
4. Click **Save**

### 8. Verify via Reports

After completing the above steps, verify data through the Reports module:

**Stock Reports:**
1. Navigate to **Reports** → **Stock Reports**
2. Select **Stock Balances** tab
3. Enter your warehouse ID
4. Should show -10 available quantity for `PROD001-STD`
5. Switch to **Stock Movements** tab
6. View ledger showing the outbound shipment

**Sales & Purchase Reports:**
1. Navigate to **Reports** → **Sales & Purchase Reports**
2. Select **Sales** tab
3. Set date range to include today
4. Group by: `DAY`
5. Should show 1 invoice with totals

**Party Reports:**
1. Navigate to **Reports** → **Party Reports**
2. Select **Balances** tab
3. Search for `Demo Customer`
4. Should show receivable balance (Invoice 1180 - Payment 500 = 680 TRY)
5. Switch to **Aging** tab
6. View aging buckets (invoice should be in 0-30 days bucket if not overdue)
   - **Note**: Aging shows gross exposure from invoices only; payment matching not yet implemented

**Cash & Bank Reports:**
1. Navigate to **Reports** → **Cash & Bank Reports**
2. Should show:
   - **Cashboxes**: Main Cashbox with balance 500 TRY
   - **Grand Total**: 500 TRY

### 9. Legacy Balance Checks

**Party Balance (Ledger View):**
1. Navigate to **Party Ledger**
2. Select `Demo Customer`
3. View balance (should show receivable from invoice minus payment)

**Cashbox Balance (Ledger View):**
1. Navigate to **Cash/Bank Ledger**
2. Select Source Type: `CASHBOX`
3. Select Source: `Main Cashbox`
4. Click **Get Balance**
5. Should show +500 from the payment

## Development

### Project Structure

```
apps/admin-desktop/
├── src/                    # React application
│   ├── components/         # UI components
│   │   ├── ui/            # Base UI components (Button, Input, Card)
│   │   └── MainLayout.tsx # Main app layout with sidebar
│   ├── pages/             # Page components
│   │   ├── LoginPage.tsx
│   │   ├── DashboardPage.tsx
│   │   ├── SettingsPage.tsx
│   │   └── reports/       # Report pages
│   │       ├── StockReportsPage.tsx
│   │       ├── SalesReportsPage.tsx
│   │       ├── PartiesReportsPage.tsx
│   │       └── CashBankReportsPage.tsx
│   ├── hooks/             # Custom React hooks
│   │   ├── useProducts.ts
│   │   ├── useParties.ts
│   │   ├── useReports.ts  # Report data hooks
│   │   └── ...
│   ├── types/             # TypeScript type definitions
│   │   ├── product.ts
│   │   ├── party.ts
│   │   ├── report.ts      # Report DTOs
│   │   └── ...
│   ├── lib/               # Utilities
│   │   ├── api-client.ts  # API client with auth
│   │   ├── settings.ts    # Settings management
│   │   └── utils.ts       # Helper functions
│   ├── App.tsx            # Main app component
│   └── main.tsx           # Entry point
├── src-tauri/             # Tauri (Rust) application
│   ├── src/
│   │   └── main.rs        # Tauri entry point
│   ├── Cargo.toml         # Rust dependencies
│   └── tauri.conf.json    # Tauri configuration
├── index.html
├── vite.config.ts
├── tailwind.config.js
└── package.json
```

## Reports Module

The Admin Console includes comprehensive operational reports accessible from the **Reports** menu:

### Stock Reports

**Stock Balances**
- **Endpoint**: `GET /reports/stock/balances`
- **Filters**:
  - `warehouseId` (required): Filter by warehouse
  - `q` (optional): Search by SKU or variant name
  - Pagination: `page`, `size` (max 200)
- **Columns**: SKU, Variant Name, Unit, On Hand, Reserved, Available
- **Use Case**: Monitor current stock levels per warehouse

**Stock Movements**
- **Endpoint**: `GET /reports/stock/movements`
- **Filters**:
  - `warehouseId` (optional): Filter by warehouse
  - `variantId` (optional): Filter by product variant
  - `movementType` (optional): IN, OUT, TRANSFER, etc.
  - `from`, `to` (optional): Date range (inclusive)
  - Pagination: `page`, `size` (max 200)
- **Columns**: Date/Time, Movement Type, Quantity, Reference Type, Reference ID, Note
- **Ordering**: Most recent first (descending by OccurredAt)
- **Use Case**: Audit stock transactions and trace inventory changes

### Sales & Purchase Reports

**Sales Summary**
- **Endpoint**: `GET /reports/sales/summary`
- **Filters**:
  - `from`, `to` (optional): Date range
  - `groupBy`: `DAY` or `MONTH`
- **Columns**: Period, Invoice Count, Total Net, Total VAT, Total Gross
- **Use Case**: Analyze sales performance over time

**Purchase Summary**
- **Endpoint**: `GET /reports/purchase/summary`
- **Filters**: Same as Sales Summary
- **Columns**: Same as Sales Summary
- **Use Case**: Track procurement spending

### Party Reports

**Party Balances**
- **Endpoint**: `GET /reports/parties/balances`
- **Filters**:
  - `q` (optional): Search by code or name
  - `type` (optional): `CUSTOMER` or `SUPPLIER`
  - `at` (optional): As-of date for point-in-time balance
  - Pagination: `page`, `size` (max 200)
- **Columns**: Code, Name, Type, Balance, Currency
- **Use Case**: Monitor customer receivables and supplier payables

**Party Aging**
- **Endpoint**: `GET /reports/parties/aging`
- **Filters**:
  - `q` (optional): Search by code or name
  - `type` (optional): `CUSTOMER` or `SUPPLIER`
  - `at` (optional): As-of date for aging calculation
  - Pagination: `page`, `size` (max 200)
- **Columns**: Code, Name, 0-30 Days, 31-60 Days, 61-90 Days, 90+ Days, Total
- **Aging Logic**:
  - Buckets based on days overdue: `(asOfDate - DueDate).Days`
  - If `DueDate` is null, uses `IssueDate`
  - Calculations based on SALES ISSUED invoices only
- **⚠️ LIMITATION**: Payment matching is **not yet implemented**. The aging report shows **gross exposure** from invoices only. It does not account for which invoices have been paid or partially paid. This means:
  - All ISSUED invoices are included in aging buckets
  - Paid invoices still appear as overdue
  - Use this report for exposure analysis, not collection status
- **Use Case**: Identify overdue invoices and prioritize collections (with caveat above)

### Cash & Bank Reports

**Cash/Bank Balances**
- **Endpoint**: `GET /reports/cashbank/balances`
- **Filters**:
  - `at` (optional): As-of date for point-in-time balance
- **Sections**:
  - **Cashboxes**: Lists all cashboxes with balances
  - **Bank Accounts**: Lists all bank accounts with balances
  - **Subtotals**: Per section (cash total, bank total)
  - **Grand Total**: Combined cash + bank balance
- **Columns**: Code, Name, Currency, Balance
- **Use Case**: Monitor liquidity and cash position

### Report Permissions

All report endpoints require the `reports.read` permission. Ensure your JWT token includes this claim.

### Pagination Notes

- Default page size: **50**
- Maximum page size: **200**
- Page numbers are 1-indexed
- Total record count is included in response for pagination controls

### Date Filtering

All date range filters (`from`, `to`) use **inclusive** logic:
- `from`: Start of day (00:00:00)
- `to`: End of day (23:59:59)
- Example: `from=2026-02-01&to=2026-02-01` returns all transactions on Feb 1, 2026

### Building for Production

```bash
npm run tauri build
```

This creates a distributable application in `src-tauri/target/release/bundle/`.

## Error Handling

The application handles common API errors:

- **401 Unauthorized**: Token is invalid or expired → redirects to login
- **409 Conflict**: Validation error (e.g., duplicate code, currency mismatch)
- **400 Bad Request**: Invalid input data
- **500 Server Error**: Backend issue

Error messages are displayed inline on forms and via toast notifications.

## Security Notes

- JWT tokens are stored using Tauri's secure store plugin
- Tokens are never exposed in the UI (no "copy token" feature)
- API calls require authentication header
- Window state is remembered but tokens are encrypted at rest

## Troubleshooting

### Backend Connection Issues

1. Verify backend is running: `curl http://localhost:5000/health`
2. Check API Base URL in Settings
3. Look for CORS errors in browser console

### Build Errors

```bash
# Clean and rebuild
rm -rf node_modules dist src-tauri/target
npm install
npm run tauri build
```

### Token Issues

1. Verify token is valid JWT format
2. Check token expiration
3. Ensure token has required permissions
4. Clear token storage and re-login

## License

Internal use only.
