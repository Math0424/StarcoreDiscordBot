using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml;

namespace StarcoreDiscordBot
{
    public class BlueprintReader
    {

		public struct BlueprintInfo
        {
            public bool FileNotFound;
            public int SubgridCount;
            public int BlockCount;
            public int BattlePoints;
            public bool HasLargeGrid;
            public bool HasSmallGrid;
            public Dictionary<string, int> Blocks;
        }


        //private const string GithubLink = "https://raw.githubusercontent.com/enenra/starcore-mods/main/StarCore%20Battle%20Points/Data/Scripts/Additions/PointAdditions.cs";



        private Dictionary<string, int> BPpoints;
        public BlueprintReader(string data)
        {
            BPpoints = new Dictionary<string, int>();

            if (data == null)
                return;

            if (data.StartsWith("https://"))
            {
                using WebClient client = new WebClient();
                using Stream stream = client.OpenRead(data);

                using StreamReader reader = new StreamReader(stream);
                string result = reader.ReadToEnd();
                reader.Close();
                stream.Close();

                Load(result);
            }
            else
            {
                Load(data);
            }
        }

        private void Load(string data)
        {
            BPpoints.Clear();

            int start = data.IndexOf('"');
            int end = data.LastIndexOf('"');
            end = (end == -1 ? data.Length : end);

            string[] lines = data.Substring(start + 1, end - start - 1).Trim().Split('\n');

            foreach (string s in lines)
            {
                string[] parts = s.Replace(';', ' ').Split('@');
                if (parts.Length == 2)
                {
                    string name = parts[0].Trim();
                    if (!BPpoints.ContainsKey(name) && int.TryParse(parts[1], out int val))
                    {
                        BPpoints.Add(name, val);
                    }
                }
            }
            Utils.Log($"Loaded {BPpoints.Count} block values");
        }

        public BlueprintInfo ReadBP(WCBlueprint blueprint)
        {
            BlueprintInfo info = new BlueprintInfo()
            {
                Blocks = new Dictionary<string, int>(),
            };

            ParseFile(ref info, ref blueprint);
            foreach (var b in info.Blocks)
            {
                if (BPpoints.ContainsKey(b.Key))
                {
                    info.BattlePoints += BPpoints[b.Key] * b.Value;
                }
            }

            return info;
        }

        private void ParseFile(ref BlueprintInfo info, ref WCBlueprint blueprint)
        {
            if (!File.Exists(blueprint.GetSBCPath()))
            {
                info.FileNotFound = true;
                return;
            }

            XmlDocument bp = new XmlDocument();
            using FileStream f = File.Open(blueprint.GetSBCPath(), FileMode.Open);
            bp.Load(f);

            //grids
            foreach (XmlNode cubeGrid in bp.GetElementsByTagName("CubeGrids")) 
            {
                info.SubgridCount++;
                foreach (XmlNode subgrid in cubeGrid.ChildNodes)
                {
                    if (subgrid["GridSizeEnum"] != null)
                    {
                        if (subgrid["GridSizeEnum"].InnerText == "Small")
                            info.HasSmallGrid = true;
                        else
                            info.HasLargeGrid = true;
                    }
                    if (subgrid["CubeBlocks"] != null)
                    {
                        foreach (XmlNode cubeBlock in subgrid["CubeBlocks"].ChildNodes)
                        {
                            info.BlockCount++;
                            if (cubeBlock["SubtypeName"] != null)
                            {
                                string type = cubeBlock["SubtypeName"].InnerText;
                                if (!string.IsNullOrEmpty(type))
                                {
                                    if (!info.Blocks.ContainsKey(type))
                                        info.Blocks.Add(type, 0);
                                    info.Blocks[type] += 1;
                                }
                            }
                        }
                    }
                }
            }
            f.Close();

        }


    }
}
