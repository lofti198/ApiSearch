namespace ApiSearchConsole.Services
{
    public class LoggerService
    {
        public void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }
    }
}