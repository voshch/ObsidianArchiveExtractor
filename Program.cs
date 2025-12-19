using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ObsidianArchiveExtractor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Check for help request
            if (args.Length > 0 && (args[0] == "-h" || args[0] == "--help" || args[0] == "/?"))
            {
                ShowHelp();
                return;
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Form1 form = new Form1();
            
            // If command-line arguments provided
            if (args.Length > 0)
            {
                string inputFile = args[0];
                string outputDir = args.Length > 1 ? args[1] : null;
                bool preserveSubdirectories = args.Contains("--subdirectories");
                
                form.AutoExtractMode(inputFile, outputDir, preserveSubdirectories);
            }
            
            Application.Run(form);
        }
        
        static void ShowHelp()
        {
            Console.WriteLine("ObsidianArchiveExtractor - Extract files from Obsidian Archive (.oaf) files\n");
            Console.WriteLine("Usage:");
            Console.WriteLine("  ObsidianArchiveExtractor.exe              - Open GUI");
            Console.WriteLine("  ObsidianArchiveExtractor.exe <file>       - Open GUI with archive loaded");
            Console.WriteLine("  ObsidianArchiveExtractor.exe <file> <dir> - Extract all files to directory and exit");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  --subdirectories  Keep directory structure from archive (default: flatten)");
            Console.WriteLine("  -h, --help, /?    Show this help message");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  ObsidianArchiveExtractor.exe archive.oaf");
            Console.WriteLine("  ObsidianArchiveExtractor.exe archive.oaf C:\\output\\files");
            Console.WriteLine("  ObsidianArchiveExtractor.exe archive.oaf C:\\output\\files --subdirectories");
        }
    }
}