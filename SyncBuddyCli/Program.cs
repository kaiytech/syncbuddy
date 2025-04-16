using CommandLine;

class Program
{
    class Options
    {
        [Option('n', "name", Required = true, HelpText = "Your name.")]
        public string Name { get; set; }

        [Option('a', "age", Required = false, HelpText = "Your age.")]
        public int Age { get; set; }
    }

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(options =>
            {
                Console.WriteLine($"Name: {options.Name}, Age: {options.Age}");
            })
            .WithNotParsed(errors =>
            {
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
            });
    }
}