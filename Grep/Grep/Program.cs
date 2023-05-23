using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Grep
{
    internal class Grep
    {
        static void Main(string[] args)
        {

            var nonSwitches = from arg in args where arg.FirstOrDefault() != '/' select arg;
            var regexString = nonSwitches.First();
            var filesArguments = nonSwitches.Skip(1);

            if (regexString == null || filesArguments.Count() == 0)
            {
                Console.WriteLine("Please provide both a search term and filepatterns");
                return;
            }

            var regex = new ThreadLocal<Regex>(() =>
            new Regex(regexString, RegexOptions.Compiled));

            var files = from fileArgument in filesArguments
                        let dirName = Path.GetDirectoryName(fileArgument)
                        let fileName = Path.GetFileName(fileArgument)
                        from file in Directory.EnumerateFiles(
                            dirName,
                            fileName, SearchOption.TopDirectoryOnly)
                        select file;


            try
            {
                var matches = from file in files.AsParallel().AsOrdered().WithMergeOptions(ParallelMergeOptions.NotBuffered)
                              from line in File.ReadLines(file).Zip(Enumerable.Range(1, int.MaxValue), (s, i) => new { Num = i, Text = s, File = file })
                              where regex.Value!.IsMatch(line.Text)
                              select line;
                foreach (var line in matches)
                {
                    Console.WriteLine($"{line.File}:{line.Num} {line.Text}");
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => { Console.WriteLine(e.Message); return true; });
            }


        }
    }
}