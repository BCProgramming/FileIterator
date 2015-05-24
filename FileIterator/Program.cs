using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BASeCamp.Search;
using BCFileSearch;

namespace FileIterator
{
    class Program
    {
        static void FileFound(FileSearchResult founddata)
        {
            Console.WriteLine("Found:" + founddata.FullPath);
            Console.WriteLine("Created:" + founddata.DateCreated.ToString());

        }
        static void Main(string[] args)
        {
            //DebugLogger.EnableLogging = true;

          

            String pfolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            AsyncFileFinder aff = new AsyncFileFinder(pfolder, "*.exe", null, null, false);
            aff.AsyncFileFound += aff_AsyncFileFound;
            aff.AsyncFileFindComplete += aff_AsyncFileFindComplete;
            aff.Start();
           
            Thread.Sleep(5000);
            {
                aff.Cancel();
            }
            
            
           
  
            Console.WriteLine("Found " + ItemCount + " Items.");
            Console.ReadKey();

        }

        static void aff_AsyncFileFindComplete(object sender, AsyncFileFindCompleteEventArgs e)
        {
            if(e.CompletionCause==AsyncFileFindCompleteEventArgs.CompletionCauseEnum.Complete_Cancelled)
            {
                Console.WriteLine("Search Cancelled");
            }
            else
            {
                Console.WriteLine("Search Completed.");
            }
        }
        static int ItemCount = 0;
        static void aff_AsyncFileFound(object sender, AsyncFileFoundEventArgs e)
        {
            ItemCount++;
            Console.WriteLine(e.Result.FullPath);
        }
        private static bool FileTest(FileSearchResult fsr)
        {
            return true;
        }

        
    }
}
