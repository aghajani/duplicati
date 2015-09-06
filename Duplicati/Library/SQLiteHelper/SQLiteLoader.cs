#region Disclaimer / License
// Copyright (C) 2015, The Duplicati Team
// http://www.duplicati.com, info@duplicati.com
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace Duplicati.Library.SQLiteHelper
{
    public static class SQLiteLoader
    {
        /// <summary>
        /// A cached copy of the type
        /// </summary>
        private static Type m_type = null;

        /// <summary>
        /// Returns the SQLiteCommand type for the current architecture
        /// </summary>
        public static Type SQLiteConnectionType
        {
            get
            {
                if (m_type == null)
                {
                    string filename = "System.Data.SQLite.dll";
                    string basePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "SQLite");

                    //Default is to use the pinvoke version which requires a native .dll/.so
                    string assemblyPath = System.IO.Path.Combine(basePath, "pinvoke");

                    if (!Duplicati.Library.Utility.Utility.IsMono)
                    {
                        //If we run with MS.Net we can use the mixed mode assemblies
                        if (Library.Utility.Utility.Is64BitProcess)
                        {
                            if (System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.Combine(basePath, "win64"), filename)))
                                assemblyPath = System.IO.Path.Combine(basePath, "win64");
                        }
                        else
                        {
                            if (System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.Combine(basePath, "win32"), filename)))
                                assemblyPath = System.IO.Path.Combine(basePath, "win32");
                        }
                    } else {
                        //On Mono, we try to find the Mono version of SQLite
                        
                        //This secret environment variable can be used to support older installations
                        var envvalue = System.Environment.GetEnvironmentVariable("DISABLE_MONO_DATA_SQLITE");
                        if (!Utility.Utility.ParseBool(envvalue, envvalue != null))
                        {
                            foreach(var asmversion in new string[] {"4.0.0.0", "2.0.0.0"})
                            {
                                try 
                                {
                                    Type t = System.Reflection.Assembly.Load(string.Format("Mono.Data.Sqlite, Version={0}, Culture=neutral, PublicKeyToken=0738eb9f132ed756", asmversion)).GetType("Mono.Data.Sqlite.SqliteConnection");
                                    if (t != null && t.GetInterface("System.Data.IDbConnection", false) != null)
                                    {
                                        Version v = new Version((string)t.GetProperty("SQLiteVersion").GetValue(null, null));
                                        if (v >= new Version(3, 6, 3))
                                        {
                                            m_type = t;
                                            return m_type;
                                        }
                                    }
                                    
                                } catch {
                                }
                            }

                            Console.WriteLine("Failed to load Mono.Data.Sqlite.SqliteConnection, reverting to built-in.");
                        }
                    }

                    m_type = System.Reflection.Assembly.LoadFile(System.IO.Path.Combine(assemblyPath, filename)).GetType("System.Data.SQLite.SQLiteConnection");
                }

                return m_type;
            }
        }
    }
}
