using lab3.Controllers;
using lab3.Data;
using lab3.Services;

namespace lab3;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var settings = DatabaseSettings.Load();
            var connectionFactory = new MySqlConnectionFactory(settings);
            DatabaseSchemaInitializer.EnsureAsync(connectionFactory).GetAwaiter().GetResult();
            var userRepository = new UserRepository(connectionFactory);
            var authenticationService = new AuthenticationService(userRepository);
            var loginController = new LoginController(authenticationService);
            var productRepository = new ProductRepository(connectionFactory);
            var productController = new ProductController(productRepository);
            var customerRepository = new CustomerRepository(connectionFactory);
            var reportRepository = new ReportRepository(connectionFactory);
            var services = new ApplicationServices
            {
                Products = productController,
                Customers = new CustomerController(customerRepository),
                Sales = new SalesService(connectionFactory),
                Expenses = new ExpenseRepository(connectionFactory),
                Reports = new ReportController(reportRepository),
                Users = userRepository,
                Payments = new CustomerPaymentService(connectionFactory)
            };

            Application.Run(new Form1(loginController, services));
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "SalesTrackRW startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
