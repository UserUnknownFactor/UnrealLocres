﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

using LocresLib;

namespace UnrealLocres.Converter
{
    public class BaseConverter
    {
        public string ExportExtension { get => "csv"; }
        public string ImportExtension { get => "csv"; }

        public void Export(LocresFile locres, string outputPath)
        {
            var data = new List<TranslationEntry>();

            foreach (var ns in locres)
            {
                foreach (var str in ns)
                {
                    if (string.IsNullOrEmpty(str.Value))
                        continue;

                    var key = (!string.IsNullOrWhiteSpace(ns.Name) ? ns.Name + "/" : "") + str.Key;
                    data.Add(new TranslationEntry(key, str.Value, string.Empty));
                }
            }

            using (var file = File.Create(outputPath))
            using (var writer = new StreamWriter(file))
            {
                Write(data, writer);
            }

            Console.WriteLine($"Exported {data.Count} strings to {outputPath}");
        }

        protected void Write(List<TranslationEntry> data, TextWriter writer)
        {
            foreach (var l in data)
            {
                writer.WriteLine(l.Source.Replace(CSVSeparator, CSVEscape + CSVSeparator).Replace("\r", "\\r").Replace("\n", "\\n") +
                    CSVSeparator + l.Target.Replace(CSVSeparator, CSVEscape + CSVSeparator).Replace("\r", "\\r").Replace("\n", "\\n") +
                    CSVSeparator + l.Key.Replace("\r", "\\r").Replace("\n", "\\n"));
            }
        }

        public void Import(LocresFile locres, string inputPath)
        {
            List<TranslationEntry> data;
            using (var file = File.OpenRead(inputPath))
            using (var reader = new StreamReader(file))
                data = Read(reader);

            var translatedList = data.Where(x => !string.IsNullOrEmpty(x.Target)).ToList();
            int total = data.Count;
            int translated = translatedList.Count;

            Console.WriteLine($"Loaded {inputPath}");
            Console.WriteLine($"Translated {translated} / {total} ({translated/total:P})");

            var dict = translatedList.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (LocresNamespace lr_ns in locres) {
                foreach (LocresString lr_str in lr_ns) {
                    var key = (!string.IsNullOrWhiteSpace(lr_ns.Name) ? lr_ns.Name + "/" : "") + lr_str.Key;
                    if (dict.TryGetValue(key, out var item)) {
                        if (string.IsNullOrEmpty(item.Target))
                            continue;

                        lr_str.Value = item.Target;
                        dict.Remove(key);
                    }
                }
            }

            foreach (LocresNamespace lr_ns in locres) {
                var newItems = new LocresNamespace();
                foreach (var item in dict)
                {
                    if (lr_ns.Name == item.Value.NameSpace) {
                        Console.WriteLine(lr_ns.Name +" : " +  item.Value.NameSpace);
                        newItems.Add(new LocresString(item.Value.Key, item.Value.Target, item.Value.SourceStringHash));
                    }
                }
                if (newItems.Count() > 0)
                    lr_ns.AddRange(newItems);
            }

            if (false && dict.Count > 0)
            {
                Console.WriteLine($"\nWARNING: {dict.Count} translations are not used.\n Please check translation namespaces/keys.");
                foreach (var kvpair in dict)
                {
                    var source = kvpair.Value.Source;
                    if (source.Length > 40)
                        source = source.Substring(0, 40) + "...";
                    source = source.Replace("\r", "\\r").Replace("\n", "\\n");
                    Console.WriteLine($"  Key \"{kvpair.Key}\" Source: \"{source}\"");
                }
            }

            Console.WriteLine($"\nImported {translated - dict.Count} translations.");
        }

        protected List<TranslationEntry> Read(TextReader stream) {
            List<TranslationEntry> list = new List<TranslationEntry>();
            string line = null;
            while ((line = stream.ReadLine()) != null) {
                string[] l = line.Split(CSVSeparator[0]);
                string merged = null;
                List<string> l2 = new List<string>();
                foreach (string l_inner in l)
                {
                    if (!string.IsNullOrEmpty(merged))
                    {
                        merged = merged + CSVSeparator + l_inner.Replace("\\r", "\r").Replace("\\n", "\n");
                        l2.Add(merged);
                        merged = null;
                        continue;
                    }
                    if (string.IsNullOrEmpty(l_inner)) {
                        l2.Add(l_inner);
                        continue;
                    }
                    if (l_inner.Substring(l_inner.Length - 1) != CSVEscape) {
                        l2.Add(l_inner.Replace("\\r", "\r").Replace("\\n", "\n"));
                    }
                    else
                    {
                        merged = l_inner.Replace("\\r", "\r").Replace("\\n", "\n");
                    }

                }
                if (l.Count() < 3) continue;
                list.Add(new TranslationEntry(l2.ToArray()));
            }
            return list;
        }

        protected class TranslationEntry
        {
            public TranslationEntry(string key, string source, string target)
            {
                Key = key;
                Source = source;
                Target = target;
            }

            public TranslationEntry(string[] csv_line)
            {
                Key = csv_line[2];
                Source = csv_line[0];
                Target = csv_line[1];
            }

            public string Key { get; set; }
            public string Source { get; set; }
            public string Target { get; set; }

            public uint SourceStringHash { get => Crc.StrCrc32(Source); }

            public string NameSpace { get {
                var ns_data = Key.Split(NSSeparator[0]);
                if (ns_data.Count() > 1)
                    return ns_data[0];
                else
                    return "";
            }}
        }

        public static string CSVSeparator { get => "→"; }
        public static string CSVEscape { get => "¶"; }
        public static string NSSeparator { get => "/"; }
    }
}
