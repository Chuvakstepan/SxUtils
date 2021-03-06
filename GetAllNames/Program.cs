﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml.Linq;

namespace GetAllNames
{
    class Program
    {
        private static string[] _dirs;
        private static List<string> _spisokPrint = new List<string>();
        private static List<string> _spisokPrintBat = new List<string>();

        static void Main(string[] args)
        {
            if (args.Contains("-update"))
            {
                UpdateAndSaveNewTitles();
                return;
            }
            
            var dlcMode = args.Contains("-dlc");
            
            var normalNames = Directory.GetCurrentDirectory() + "\\filenames.txt";
            var dlcNames = Directory.GetCurrentDirectory() + "\\dlcnames.txt";

            var textfilepath = dlcMode ? dlcNames : normalNames;

            if (File.Exists(textfilepath))
                File.Delete(textfilepath);
            
            _dirs = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.nsp");

            if (dlcMode)
                GetDlcNames();
            else
                GetNormalNames();

            SaveNamesToFile(textfilepath);

            Console.WriteLine("Список файлов записан в " + textfilepath);
            Console.WriteLine("Нажмите любую клавишу для выхода");
            Console.ReadKey();
        }

        private static void SaveNamesToFile(string filepath)
        {
            foreach (var filename in _spisokPrint)
            {
                Console.WriteLine(filename);
                var appendText = filename + Environment.NewLine;
                File.AppendAllText(filepath, appendText, Encoding.UTF8);
            }
        }

        private static void SaveBatToCDNSP(string batFile)
        {
            foreach (var str1 in _spisokPrintBat)
            {
                var appendText = str1 + Environment.NewLine;
                File.AppendAllText(batFile, appendText, new UTF8Encoding(false));
            }
            File.AppendAllText(batFile, "pause", new UTF8Encoding(false));



        }

        private static void GetDlcNames()
        {
            foreach (var dir in _dirs)
            {
                var filename = Path.GetFileName(dir);
                
                if (filename != null && filename.Contains("[DLC]") && _spisokPrint.IndexOf(filename) == -1)
                    _spisokPrint.Add(filename);
            }
        }

        private static void GetNormalNames()
        {
            foreach (var dir in _dirs)
            {
                var filename = Path.GetFileName(dir);

                if (filename == null || filename.Contains("[DLC]"))
                    continue;

                filename = filename.Replace(".nsp", "").Trim();

                while (filename.IndexOf("[") > -1)
                {
                    var nachalo = filename.IndexOf("[");
                    var conchalo = filename.IndexOf("]");
                    var textstrip = filename.Substring(nachalo, conchalo - nachalo + 1);
                    filename = filename.Replace(textstrip, "").Trim();
                }

                if (filename == "")
                    continue;

                if (_spisokPrint.IndexOf(filename) == -1)
                    _spisokPrint.Add(filename);
            }
        }

        private static void UpdateAndSaveNewTitles()
        {
            const string dbUrl = "http://snip.li/newkeydb";
            
            WebRequest request = WebRequest.Create(dbUrl);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader (dataStream);
            string responseFromServer = reader.ReadToEnd();
            
            reader.Close ();
            dataStream.Close ();
            response.Close ();

            var updatedData = responseFromServer.Split('\n');
            var updatedTitles = updatedData.Select(title => title.Replace("\n", "").Trim());

            var currentTitlesFile = Directory.GetCurrentDirectory() + "\\titlekeys.txt";
            var newTitlesFile = Directory.GetCurrentDirectory() + "\\newtitlekeys.txt";
            var batFile = Directory.GetCurrentDirectory() + "\\start_update.bat";

            if (File.Exists(newTitlesFile))
                File.Delete(newTitlesFile);
            if (File.Exists(batFile))
                File.Delete(batFile);

            if (!File.Exists(currentTitlesFile))
            {
                Console.WriteLine("Файл titlekeys.txt не найден!");
                Console.WriteLine("Нажмите любую клавишу для выхода");
                Console.ReadKey();                 
                return;
            }

            var currentTitles = File.ReadAllLines(currentTitlesFile);
            var filteredTitles = new List<string>(updatedTitles.Except(currentTitles));

            var exceptionXml = GetNswdbXml();

            foreach (var title in filteredTitles)
            {
                var splitter = title.IndexOf('|');
                if (splitter<1)
                {
                    continue;
                }
                var titleId = title.Substring(0, splitter);
                var titleKey = title.Substring(splitter + 1, 32);

                if (!exceptionXml.Contains(titleId))
                    _spisokPrint.Add(title);
                    _spisokPrintBat.Add("CDNSP.py -r -g " + titleId + "-0-"+titleKey);
            }
            if (_spisokPrint.Count()==0)
            {
                Console.WriteLine("Обновлений тайтлов не найдено");
                Console.WriteLine("Нажмите любую клавишу для выхода");
                Console.ReadKey();
                return;
            }
            SaveNamesToFile(newTitlesFile);

            Console.WriteLine("Список новых тайтлов записан в " + newTitlesFile);
            Console.WriteLine("Нажмите Y для формирования исполняемого файла для CDNSP, нажмите любую другую клавишу для выхода");
            if (Console.ReadKey().Key==ConsoleKey.Y)
            {
                SaveBatToCDNSP(batFile);
                Console.WriteLine("");
                Console.WriteLine("Файл start_update.bat сформирован");
                Console.WriteLine("После запуска CDNSP и успешной скачки тайтлов нажмите Y для того чтобы объединить newtitlekeys.txt и titlekeys.txt");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    MergeTitleKeys(newTitlesFile, currentTitlesFile);
                    Console.WriteLine("Файлы успешно объединены");
                }
                Console.WriteLine("Нажмите любую клавишу для выхода");
                Console.ReadKey();

            }
        }

        private static void MergeTitleKeys(string newTitlesfile, string currentTitlesFile)
        {
            string[] newtitles = File.ReadAllLines(newTitlesfile);
            foreach (string s in newtitles)
            {
                File.AppendAllText(currentTitlesFile, s + Environment.NewLine);
            }
            File.Delete(newTitlesfile);
        }


        private static List<string> GetNswdbXml()
        {
            const string xmlUrl = "http://nswdb.com/xml.php";

            WebRequest requestXml = WebRequest.Create(xmlUrl);
            WebResponse responseXml = requestXml.GetResponse();
            Stream dataStreamXml = responseXml.GetResponseStream();
            StreamReader readerXml = new StreamReader (dataStreamXml);
            string responseFromServerXml = readerXml.ReadToEnd();
            
            readerXml.Close ();
            dataStreamXml.Close ();
            responseXml.Close ();

            var doc = XDocument.Parse(responseFromServerXml);

            if (doc.Root == null)
                return null;

            var rawRelease = doc.Root.Elements("release");
            var cartridgeTitlesId = rawRelease.Select(a => a.Element("titleid").Value).ToList();

//            foreach (var v in cartridgeTitlesId)
//                Console.WriteLine(v);

            return cartridgeTitlesId;
        }
    }
}