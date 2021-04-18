using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace tModLoaderStats
{
    class Program
    {
        static void Main(string[] args)
        {
            ScrapeMods();
        }

        private static void ScrapeMods()
        {
            Console.WriteLine("Waiting for javid.ddns.net...");
            var website = GetHtmlAsync("http://javid.ddns.net/tModLoader/modmigrationprogressalltime.php").Result;

            Console.WriteLine("Parse-ing HTML");
            var decendants = website.DocumentNode.Descendants("table").ToArray()[0];
            var list = decendants.Descendants("tr").ToArray();

            var modList = new List<(string FullName, int DownloadsTotal, int DownloadsYesterday)>();

            Console.WriteLine("Getting Data");
            for (int i = 1; i < list.Length; i++)
            {
                var iteminfo = list[i].Descendants("td").ToArray();

                var mod = (
                    FullName: iteminfo[1].InnerText,
                    DownloadsTotal: int.Parse(iteminfo[2].InnerText),
                    DownloadsYesterday: int.Parse(iteminfo[3].InnerText)
                );
                modList.Add(mod);
            }
            Console.WriteLine("Done");

            ShowResults(modList);

            SaveData(modList);
        }

        private static void ShowResults(List<(string FullName, int DownloadsTotal, int DownloadsYesterday)> modList)
        {
            Console.Clear();
            Console.WriteLine("There are " + modList.Count + " mods in the modbrowser");
            Console.WriteLine("The mods with the most downloads are: \n");

            for (int i = 0; i <= 10; i++)
            {
                Console.WriteLine($"{i + 1}. {modList[i].FullName} : {modList[i].DownloadsTotal}");
            }

            Console.WriteLine("\nThe download count of all mods combined is: " + modList.Sum(x => x.DownloadsTotal));
            Console.WriteLine("the average download count is: " + modList.Average(x => x.DownloadsTotal));
            Console.WriteLine("the median download count is: " + modList.Median(x => x.DownloadsTotal));
            Console.WriteLine("There are " + modList.Where(x => x.DownloadsYesterday > 5).LongCount() + " dead mods");
        }

        private static void SaveData(List<(string FullName, int DownloadsTotal, int DownloadsYesterday)> modList)
        {
            DirectoryInfo Folder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent;
            string FolderPath = Folder.FullName.Remove(Folder.Name);

            if (!Directory.Exists(FolderPath + "data"))
                Directory.CreateDirectory(FolderPath + "data");

            string csv = "Rank,Display Name,Downloads Total,Downloads Yesterday\n";
            for (int i = 0; i < modList.Count; i++)
            {
                csv += $"{i + 1},{modList[i].FullName.Escape()},{modList[i].DownloadsTotal},{modList[i].DownloadsYesterday}\n";
            }

            File.WriteAllText(FolderPath + @$"data/data_{DateTime.Now.ToShortDateString()}.csv", csv);
        }

        private static async Task<HtmlAgilityPack.HtmlDocument> GetHtmlAsync(string url)
        {
            var html = await new HttpClient().GetStringAsync(url);

            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(html);

            return htmlDocument;
        }
    }
    public static class Extentions
    {
        public static T Median<T>(this IEnumerable<T> list)
        {
            return list.ElementAt(list.Select(x => list.Count() / 2).First());
        }

        public static long Median<T>(this List<T> list, Func<T, long> selector)
        {
            return Median(list.Select(selector));
        }

        public static string Remove(this string s, string stringToRemove)
        {
            return s.Replace(stringToRemove, "");
        }

        public static string Escape(this string s)
        {
            return s.Replace("\"", @" ").Replace(","," ");
        }
    }
}
