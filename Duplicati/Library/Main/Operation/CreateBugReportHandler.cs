//  Copyright (C) 2013, The Duplicati Team

//  http://www.duplicati.com, opensource@duplicati.com
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using Duplicati.Library.Main.Database;

namespace Duplicati.Library.Main.Operation
{
	internal class CreateBugReportHandler
	{
        private string m_targetpath;
        private Options m_options;
        private CreateLogDatabaseResults m_result;

		public CreateBugReportHandler(string targetpath, Options options, CreateLogDatabaseResults result)
		{
            m_targetpath = targetpath;
            m_options = options;
            m_result = result;
		}
		
		public void Run()
        {
            var ext = System.IO.Path.GetExtension(m_targetpath);
            var module = m_options.CompressionModule;
            
            if (ext != module)
                m_targetpath = m_targetpath + "." + module;
        
            if (System.IO.File.Exists(m_targetpath))
                throw new Exception(string.Format("Output file already exists, not overwriting: {0}", m_targetpath));
				
            if (!System.IO.File.Exists(m_options.Dbpath))
                throw new Exception(string.Format("Database file does not exist: {0}", m_options.Dbpath));
				
            m_result.AddMessage("Scrubbing filenames from database, this may take a while, please wait");

            using(var tmp = new Library.Utility.TempFile())
            {
                System.IO.File.Copy(m_options.Dbpath, tmp, true);
                using(var db = new LocalBugReportDatabase(tmp))
                {
                    m_result.SetDatabase(db);
                    db.Fix();
                }
                
                using(var cm = DynamicLoader.CompressionLoader.GetModule(module, m_targetpath, m_options.RawOptions))
                {
                    using(var cs = cm.CreateFile("log-database.sqlite", Duplicati.Library.Interface.CompressionHint.Compressible, DateTime.UtcNow))
                    using(var fs = System.IO.File.Open(tmp, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                        Library.Utility.Utility.CopyStream(fs, cs);
                        
                    using(var cs = new System.IO.StreamWriter(cm.CreateFile("system-info.txt", Duplicati.Library.Interface.CompressionHint.Compressible, DateTime.UtcNow)))
                    {
                        cs.WriteLine("Duplicati: {0} ({1})", Duplicati.Library.Utility.Utility.getEntryAssembly().FullName, System.Reflection.Assembly.GetExecutingAssembly().FullName);
                        cs.WriteLine("OS: {0}", Environment.OSVersion);
                        cs.WriteLine("Uname: {0}", Duplicati.Library.Utility.Utility.UnameAll);
                        
                        cs.WriteLine("64bit: {0} ({1})", Environment.Is64BitOperatingSystem, Environment.Is64BitProcess);
                        cs.WriteLine("Machinename: {0}", Environment.MachineName);
                        cs.WriteLine("Processors: {0}", Environment.ProcessorCount);
                        cs.WriteLine(".Net Version: {0}", Environment.Version);
                        cs.WriteLine("Mono: {0} ({1}) ({2})", Duplicati.Library.Utility.Utility.IsMono, Duplicati.Library.Utility.Utility.MonoVersion, Duplicati.Library.Utility.Utility.MonoDisplayVersion);
                        cs.WriteLine("Locale: {0}, {1}", System.Threading.Thread.CurrentThread.CurrentCulture, System.Threading.Thread.CurrentThread.CurrentUICulture);

                        Type sqlite = null;
                        string sqliteversion = "";

                        try { sqlite = Duplicati.Library.SQLiteHelper.SQLiteLoader.SQLiteConnectionType; }
                        catch { }
                        
                        if (sqlite != null)
                        {
                            try { sqliteversion = (string)sqlite.GetProperty("SQLiteVersion").GetValue(null, null); }
                            catch { }
                            
                            cs.WriteLine("SQLite: {0} - {1}", sqliteversion, sqlite.FullName);
                        }
                    }
                }

                m_result.TargetPath = m_targetpath;
            }				
		}
	}
}

