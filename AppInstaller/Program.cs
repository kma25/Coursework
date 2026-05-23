using System.Text;
using AppInstaller.Services;

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
    Console.WriteLine("Пример: AppInstaller --download-url https://server/app.zip --target C:\\Apps\\ItSupport --main-pid 1234 --app MainApp.exe");
}
