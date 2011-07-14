﻿/*   
TShock, a server mod for Terraria
Copyright (C) 2011 The TShock Team

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Data;
using TShockAPI.DB;
using Community.CsharpSqlite.SQLiteClient;
using Microsoft.Xna.Framework;
using Terraria;


namespace TShockAPI.DB
{
    public class RegionManager
    {
        public static List<Region> Regions = new List<Region>();

        public Region[] RegionArray;

        private IDbConnection database;

        public RegionManager(IDbConnection db)
        {
            database = db;

            using (var com = database.CreateCommand())
            {
                if (TShock.Config.StorageType.ToLower() == "sqlite")
                    com.CommandText =
                        "CREATE TABLE IF NOT EXISTS 'Regions' ('X1' NUMERIC, 'Y1' NUMERIC, 'height' NUMERIC, 'width' NUMERIC, 'RegionName' TEXT PRIMARY KEY, 'WorldID' TEXT, 'UserIds' TEXT, 'Protected' NUMERIC);";
                else if (TShock.Config.StorageType.ToLower() == "mysql")
                    com.CommandText =
                        "CREATE TABLE IF NOT EXISTS Regions (X1 INT(11), Y1 INT(11), height INT(11), width INT(11), RegionName VARCHAR(255) PRIMARY, WorldID VARCHAR(255), UserIds VARCHAR(255), Protected INT(1));";

                com.ExecuteNonQuery();
            }
        }

        public void ReloadAllRegions()
        {
            try
            {
                using (var com = database.CreateCommand())
                {
                    com.CommandText = "SELECT * FROM Regions WHERE WorldID=@worldid";
                    com.AddParameter("@worldid", Main.worldID.ToString());
                    using (var reader = com.ExecuteReader())
                    {
                        int regionCount = reader.RecordsAffected;
                        RegionArray = new Region[regionCount];
                        int iterationCounter = 0;
                        while (reader.Read())
                        {
                            int X1 = reader.Get<int>("X1");
                            int Y1 = reader.Get<int>("Y1");
                            int height = reader.Get<int>("height");
                            int width = reader.Get<int>("width");
                            int Protected = reader.Get<int>("Protected");
                            string MergedIDs = DbExt.Get<string>(reader, "UserIds");
                            string name = DbExt.Get<string>(reader, "RegionName");

                            string[] SplitIDs = MergedIDs.Split(',');

                            Region r = new Region(new Rectangle(X1, Y1, width, height), name, Protected, Main.worldID.ToString());
                            RegionArray[iterationCounter] = r;
                            iterationCounter++;
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public bool AddRegion(int tx, int ty, int width, int height, string regionname, string worldid)
        {
            try
            {
                using (var com = database.CreateCommand())
                {
                    com.CommandText =
                        "INSERT INTO Regions VALUES (@tx, @ty, @height, @width, @name, @worldid, @userids, @protected);";
                    com.AddParameter("@tx", tx);
                    com.AddParameter("@ty", ty);
                    com.AddParameter("@height", width + tx);
                    com.AddParameter("@width", height + ty);
                    com.AddParameter("@name", regionname.ToLower());
                    com.AddParameter("@worldid", worldid);
                    com.AddParameter("@userids", "");
                    com.AddParameter("@protected", 1);
                    if (com.ExecuteNonQuery() > 0)
                        return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        public bool DeleteRegion(string name)
        {
            try
            {
                using (var com = database.CreateCommand())
                {
                    com.CommandText = "DELETE FROM Regions WHERE RegionName=@name AND WorldID=@worldid";
                    com.AddParameter("@name", name.ToLower());
                    com.AddParameter("@worldid", Main.worldID.ToString());
                    com.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        public bool SetRegionState(string name, bool state)
        {
            try
            {
                using (var com = database.CreateCommand())
                {
                    com.CommandText = "UPDATE Regions SET Protected=@bool WHERE RegionName=@name WorldID=@worldid";
                    com.AddParameter("@name", name);
                    if (state)
                        com.AddParameter("@bool", 1);
                    else
                        com.AddParameter("@bool", 0);
                    com.AddParameter("@worldid", Main.worldID.ToString());
                    if (com.ExecuteNonQuery() > 0)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }

        public bool InProtectedArea(int X, int Y, User user) //This whole thing is dumb
        {
            Rectangle r = new Rectangle(X, Y, 0, 0);
            for (int i = 0; i < RegionArray.Length; i++)
            {
                if (RegionArray[i].RegionArea.Intersects(r))
                {
                    for (int j = 0; j < RegionArray[i].RegionAllowedIDs.Length; j++)
                    {
                        if (RegionArray[i].RegionAllowedIDs[j] == user.Name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static List<string> ListIDs(string MergedIDs)
        {
            List<string> SplitIDs = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < MergedIDs.Length; i++)
            {
                char c = MergedIDs[i];

                if (c != ',')
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0)
                {
                    SplitIDs.Add(sb.ToString());
                    sb.Clear();
                }
            }
            return SplitIDs;
        }

        public bool AddNewUser(string regionName, User user)
        {
            string MergedIDs = string.Empty;

            try
            {
                using (var com = database.CreateCommand())
                {
                    com.CommandText = "SELECT * FROM Regions WHERE RegionName=@name AND WorldID=@worldid";
                    com.AddParameter("@name", regionName);
                    com.AddParameter("@worldid", Main.worldID.ToString());

                    using (var reader = com.ExecuteReader())
                    {
                        if (reader.Read())
                            MergedIDs = reader.Get<string>("UserIds");
                    }

                    if (MergedIDs == string.Empty)
                        MergedIDs = user.ID.ToString();
                    else
                        MergedIDs = MergedIDs + "," + user.ID;

                    com.CommandText = "UPDATE Regions SET UserIds=@ids";
                    com.AddParameter("@ids", MergedIDs);
                    return (com.ExecuteNonQuery() > 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }

        public List<Region> ListAllRegions()
        {
            List<Region> Regions = new List<Region>();
            try
            {
                using (var com = database.CreateCommand())
                {
                    com.CommandText = "SELECT * FROM Regions";
                    using (var reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                            Regions.Add(new Region(new Rectangle(reader.Get<int>("X1"), reader.Get<int>("Y1"), reader.Get<int>("height"), reader.Get<int>("width")), reader.Get<string>("RegionName"), reader.Get<int>("Protected"), reader.Get<string>("WorldID")));
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return Regions;
        }
    }

    public class Region
    {
        public Rectangle RegionArea { get; set; }
        public string RegionName { get; set; }
        public int DisableBuild { get; set; }
        public string RegionWorldID { get; set; }
        public string[] RegionAllowedIDs { get; set; }

        public Region(Rectangle region, string name, int disablebuild, string RegionWorldIDz)
        {
            RegionArea = region;
            RegionName = name;
            DisableBuild = disablebuild;
            RegionWorldID = RegionWorldIDz;
        }

        public Region()
        {
            RegionArea = Rectangle.Empty;
            RegionName = string.Empty;
            DisableBuild = 1;
            RegionWorldID = string.Empty;
        }

        public bool InProtectedArea(Rectangle point, User user)
        {
            if (RegionArea.Intersects(point))
            {
                //Todo: Code here
                //Apparently we don't have access to allowed/denied user IDs here, or they aren't constructed or something

            }
            return false;
        }
    }
}
