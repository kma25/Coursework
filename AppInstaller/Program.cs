using System.Text;
using AppInstaller.Services;

namespace AppInstaller
{
    /// <summary>
    /// Точка входа установщика, который заменяет файлы приложения после загрузки ZIP-архива.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Разбирает параметры запуска и выполняет обновление установленной копии приложения.
        /// </summary>
        /// <param name="args">Аргументы командной строки установщика.</param>
        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            try
            {
                var options = InstallerOptions.Parse(args);
                await new InstallerOrchestrator().RunAsync(options);
                Console.WriteLine("Обновление завершено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Установщик завершился ошибкой: {ex.Message}");
                Console.WriteLine("Пример: AppInstaller --download-url https://server/app.zip --target C:\\Apps\\Coursework --main-pid 1234 --app MainApp.exe");
            }
        }
    }
}
