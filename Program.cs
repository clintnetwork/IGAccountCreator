namespace IGAccountCreator
{
    static class Program
    {
        static void Main(string[] args)
        {
            var automator = new InstagramAutomator();
            automator.LoadCsv("input.csv");
            automator.Run();
;        }
    }
}