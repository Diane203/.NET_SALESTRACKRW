# SalesTrackRW

SalesTrackRW is a .NET 10 Windows Forms application for sales, inventory, customers, credit, expenses, and reporting. The project follows an MVC-style structure:

- `Models` contains domain models.
- `Views` contains WinForms screens.
- `Controllers` coordinates views and services.
- `Services` contains business rules.
- `Data` contains MySQL repositories and connection infrastructure.
- `Database/schema.sql` contains the XAMPP database schema.

## Requirements

- Visual Studio Community 2026 with the .NET desktop development workload
- .NET 10 SDK
- XAMPP with MySQL or MariaDB enabled

## XAMPP database setup

1. Start Apache and MySQL in the XAMPP Control Panel. Apache is only needed for phpMyAdmin.
2. Open `http://localhost/phpmyadmin`.
3. Select the **Import** tab.
4. Choose `lab3/Database/schema.sql` and click **Import**.
5. Confirm that the `salestrackrw` database and its tables were created.

If the database was imported before Retail/Wholesale support was added, run `lab3/Database/migration_retail_sales.sql` in phpMyAdmin. This adds the `sale_type` column and the Cashier demo account without deleting existing data.

The default XAMPP connection expected by the application is:

```text
Server=localhost;Port=3306;Database=salestrackrw;User ID=root;Password=;SslMode=None;Allow User Variables=True;
```

If the XAMPP MySQL port or root password is different, edit `lab3/appsettings.json` before running the application.

## Default login

- Username: `admin`
- Password: `admin123`
- Role: Administrator

Salesperson/Cashier demo login:

- Username: `cashier`
- Password: `cashier123`
- Role: Cashier

The Cashier sees the same operational dashboard modules for sales, products, customers, expenses, and reports. User Accounts remains restricted to Administrators.

Change this password when user-account management is available in the application. Do not use the default password in a production deployment.

## Running the application

1. Import the SQL script.
2. Confirm `appsettings.json` matches the local XAMPP credentials.
3. Open `lab3.slnx` in Visual Studio.
4. Build and run the `lab3` project.
5. Sign in with the default administrator account.

## Lecturer or new-user registration

The login screen includes **Create a new account**. A lecturer can use it without knowing the demo credentials:

1. Select **Create a new account**.
2. Enter a full name, username, password, and password confirmation.
3. Choose a privilege: **Administrator** or **Cashier**.
4. Select **Create account**.
5. Return to the login form and enter the new password.
6. Sign in using the newly created username.

The selected privilege is stored in the database. Administrators receive full management access, while Cashier accounts represent Salesperson users and do not receive User Accounts access.

## Using the application

After signing in, the dashboard provides these modules:

- **Sales** - add products to a cart, select a customer, record payment, validate stock, and complete the sale.
- **Products & Stock** - search products, view low-stock highlighting, and add products.
- **Customers & Credit** - search and create customer records.
- **Expenses** - filter expenses by date and record business expenses.
- **Reports** - view sales totals, transaction counts, low-stock items, and outstanding credit for a selected period.
- **User Accounts** - available to Administrators for creating and deactivating accounts.

Each module opens as a separate working window. Select **← Dashboard** to return without logging out, or press **Escape**. Select **Logout** on the dashboard only when you want to end the current session.

## Current implemented functionality

- MySQL/MariaDB connection through MySqlConnector
- MVC-style project structure
- Role-aware login and dashboard
- Administrator and Cashier role model
- Product and category lookup
- Product creation and search by code/name
- Retail and Wholesale sale selection
- Administrator product editing and safe deactivation
- Product pricing validation: selling price cannot be below purchase price
- Stock receiving and stock movement recording
- Low-stock highlighting
- Customer search and creation screen
- Transactional sales service with stock locking and insufficient-stock prevention
- Paid, partially-paid, and credit sale status calculation
- Customer payment recording
- Expense persistence, entry screen, and date filtering
- Dashboard/report summary screen with date filtering

The project must be connected to XAMPP to execute database operations. The solution has been verified with a successful .NET build.

## Uploading to GitHub

From the solution directory in PowerShell:

```powershell
git init
git add .
git commit -m "Implement SalesTrackRW WinForms application"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPOSITORY.git
git push -u origin main
```

Replace `YOUR_USERNAME/YOUR_REPOSITORY` with the repository URL created on GitHub. Do not commit production database passwords or other secrets.
