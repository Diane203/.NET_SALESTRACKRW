# SalesTrackRW

**Name:** IGIRANEZA Diane  
**Student ID:** 27895  
**Course:** .NET  
**Group:** Tuesday D

## Project Description

SalesTrackRW is a Sales Tracking and Business Management System for small and medium-sized businesses in Rwanda.

The system uses a role-based workflow. An Administrator manages users, products, stock, categories, customers, and business records, while a Cashier records daily sales. SalesTrackRW automatically calculates totals, checks stock availability, records paid, partially paid, or credit sales, updates customer balances, and reduces stock after each completed sale.

The project also identifies low-stock products, records expenses, maintains stock movement history, records customer payments, and provides date-filtered business reports. It brings sales, inventory, payments, customers, expenses, and reporting into one simple application.

## Project Goal

The goal of SalesTrackRW is to replace manual sales and stock records with one organized system that reduces mistakes and helps small and medium-sized businesses monitor sales, inventory, customer credit, expenses, and business performance.

## Functional Requirements

- FR-01: Users log in and log out using Administrator or Cashier roles with role-based access.
- FR-02: Administrators create, view, update, and deactivate user accounts.
- FR-03: Administrators create, view, update, and deactivate products and categories.
- FR-04: Authorized users search products by name or code and filter them by category.
- FR-05: Authorized users receive and adjust stock with a reason and view stock movement history.
- FR-06: The system identifies products at or below their configured minimum stock level.
- FR-07: Cashiers create sales with one or more products and quantities, calculate totals, and record the transaction number, date, and responsible staff member.
- FR-08: Sales record paid, partially paid, or credit status, payment method, and amount received.
- FR-09: The system prevents sales when stock is insufficient and reduces inventory after successful sales.
- FR-10: Authorized users create, view, update, search, and manage customer records.
- FR-11: The system tracks customer credit balances and payments against outstanding balances.
- FR-12: Administrators record, view, update, search, and delete business expenses by date, category, description, and amount.
- FR-13: The dashboard provides sales totals, transaction counts, low-stock items, outstanding credit, and daily, weekly, monthly, and yearly reports.
- FR-14: Required fields, quantities, monetary values, unique identifiers, and success or error messages are validated.

## Project Structure

- `lab3/Models` — business data models.
- `lab3/Views` — application screens.
- `lab3/Controllers` — communication between screens and services.
- `lab3/Services` — sales, authentication, password, and payment operations.
- `lab3/Data` — database connection and repository operations.
- `lab3/Database` — database schema and migration scripts.

## Database Setup

1. Start MySQL from XAMPP.
2. Open phpMyAdmin.
3. Import `lab3/Database/schema.sql`.
4. Import `lab3/Database/migration_retail_sales.sql` if migrating an earlier database.
5. Confirm `lab3/appsettings.json` contains the correct MySQL server, port, database, and credentials.

Database scripts include the users, categories, products, stock movements, customers, sales, sale items, customer payments, and expenses required by SalesTrackRW.

## Running the Application

1. Open `lab3.slnx` in Visual Studio 2026.
2. Restore and build the solution.
3. Start MySQL and configure `lab3/appsettings.json`.
4. Run the application and create or use an Administrator or Cashier account.

## Conclusion

SalesTrackRW gives small and medium-sized businesses in Rwanda one simple system to manage sales, stock, customers, payments, and expenses. It reduces manual errors and helps business owners make better decisions through clear reports and accessible business information.
