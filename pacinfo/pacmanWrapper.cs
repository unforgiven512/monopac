//  
//  pacmanWrapper.cs
//  
//  Author:
//       Isenmann Daniel <daniel.isenmann@gmx.de>
// 
//  Copyright (c) 2011 
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace pacinfo
{
	public class pacmanWrapper
	{
		#region delegates to inform MainWindow about pacman's progress
		public delegate void AllInstalledPackagesFinished(List<string> allPackages);
		public event AllInstalledPackagesFinished OnAllInstalledPackagesFinished;
		
		public delegate void PackageInformationFinished(packageInformation info);
		public event PackageInformationFinished OnPackageInformationFinished;
		#endregion
		
		List<string> allPackages = new List<string>();
		
		#region Threads which are doing the whole job	
		System.Threading.Thread getInstalledPackagesThread;
		System.Threading.Thread getPackageInfoThread;
		#endregion
		
		bool pacmanExists = false;
		
		
		public pacmanWrapper ()
		{
			if (File.Exists ("/usr/bin/pacman"))
				pacmanExists = true;
		}
		
		#region methods for the MainWindow to retrieve the information
		public void getAllInstalledPackagesAsync ()
		{
			if (pacmanExists) 
			{
				if (getInstalledPackagesThread != null && getInstalledPackagesThread.IsAlive)
				{
					getInstalledPackagesThread.Abort ();
				}
			
				getInstalledPackagesThread = new System.Threading.Thread (new System.Threading.ThreadStart (this.getInstalledPackagesDoWork));
				getInstalledPackagesThread.IsBackground = true;
				getInstalledPackagesThread.Start ();
			}
		}
		
		public void getInformationOfPackageAsync (string packageName)
		{
			if (pacmanExists)
			{
				if (getPackageInfoThread != null)
				{
					if (getPackageInfoThread.IsAlive)
					{
						getPackageInfoThread.Abort ();
					}
				
					if (getPackageInfoThread.ThreadState == System.Threading.ThreadState.Stopped)
					{
						getPackageInfoThread = null;
					}
				}
			
			
				getPackageInfoThread = new System.Threading.Thread (new System.Threading.ParameterizedThreadStart (getPackageInfoDoWork));
				getPackageInfoThread.IsBackground = true;
				getPackageInfoThread.Start (packageName);
			}
		}
		#endregion
		
		#region Backgroundworker methods
		#region read all installed packages
		void getInstalledPackagesDoWork ()
		{
			Process pacmanProcess = new Process ();
			ProcessStartInfo startInfo = new ProcessStartInfo ("/usr/bin/pacman", "-Qs");
			startInfo.EnvironmentVariables.Remove ("LC_ALL"); //delete old LC_ALL for this process...
			startInfo.EnvironmentVariables.Add ("LC_ALL", "C"); //and set the standard to get english output
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			pacmanProcess.StartInfo = startInfo;
			bool ret = pacmanProcess.Start ();
			
			string text = String.Empty;
			StreamReader reader;
			
			
			//if the process start was successful, read the output and put it into the package list
			if (ret) {
				reader = pacmanProcess.StandardOutput;
				if (reader != null) {
					while ((text = reader.ReadLine ()) != null) 
					{
						if (text.Contains ("local/")) {
							string[] package = text.Split ('/');
							string[] packageName = package[1].Split (' ');
							allPackages.Add(packageName[0]);
						}
					}
				}
				pacmanProcess.WaitForExit ();
			}
			pacmanProcess.Close ();
			
			if(OnAllInstalledPackagesFinished != null)
			{
				OnAllInstalledPackagesFinished(allPackages);	
			}	
		}
		
		void getInstalledPackagesCompleted (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			if ((e.Cancelled == true))
            {
                this.allPackages.Clear();
            }
			else
			{
				if(OnAllInstalledPackagesFinished != null)
				{
					OnAllInstalledPackagesFinished(allPackages);	
				}	
			}
		}
		#endregion
		
		#region read package information
		void getPackageInfoDoWork (object package)
		{		
			#region read package information
			packageInformation info = new packageInformation();
			
			string packageName = (string)package;
			try
			{
				HttpWebRequest myReq =(HttpWebRequest)WebRequest.Create("http://screenshots.debian.net/thumbnail/"+packageName);				
				HttpWebResponse response = (HttpWebResponse)myReq.GetResponse();
				
				if(response.StatusCode == HttpStatusCode.OK)
				{
					if(info != null)
					{
						using(Gdk.Pixbuf oldPixBuf = info.screenshot.Pixbuf){
							info.screenshot.Pixbuf = new Gdk.Pixbuf(response.GetResponseStream());	
						}
					}
				}
			}
			catch(Exception)
			{
			}
			
			Process pacmanInfoProcess = new Process ();
			ProcessStartInfo startInfo = new ProcessStartInfo ("/usr/bin/pacman", "-Qi " + packageName);
			startInfo.EnvironmentVariables.Remove ("LC_ALL"); //delete old LC_ALL for this process...
			startInfo.EnvironmentVariables.Add ("LC_ALL", "C"); //and set the standard to get english output
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			pacmanInfoProcess.StartInfo = startInfo;
			bool ret = pacmanInfoProcess.Start ();
			
			string text = String.Empty;
			StreamReader reader;
			
			if (ret) {
				reader = pacmanInfoProcess.StandardOutput;
				if (reader != null) {
					while ((text = reader.ReadLine ()) != null) 
					{
						int firstColon = text.IndexOf (':');
						if (firstColon > 0) {
							string labelText = text.Remove (0, firstColon + 2);
							
							if (text.Contains ("Name")) {
								info.Name = labelText;
							} else if (text.Contains ("Version")) {
								info.version = labelText;
							} else if (text.Contains ("URL")) {
								info.URL = labelText;
							} else if (text.Contains ("Description")) {
								info.Description = labelText;
							} else if (text.Contains ("Packager")) {
								info.Packager = labelText;
							} else if (text.Contains ("Install Date")) {
								info.InstallationDate = labelText;
							}
						}
					}
				}
				pacmanInfoProcess.WaitForExit ();
			}
			pacmanInfoProcess.Close ();
			#endregion
			
			#region read package content
			Process pacmanContentProcess = new Process ();
			ProcessStartInfo startContentInfo = new ProcessStartInfo ("/usr/bin/pacman", "-Ql " + packageName);
			startContentInfo.UseShellExecute = false;
			startContentInfo.CreateNoWindow = true;
			startContentInfo.UseShellExecute = false;
			startContentInfo.RedirectStandardOutput = true;
			pacmanContentProcess.StartInfo = startContentInfo;
			ret = pacmanContentProcess.Start ();
			
			text = String.Empty;
			StreamReader readerContent;
			
			info.PackageContent.Clear();
			
			if (ret) 
			{
				readerContent = pacmanContentProcess.StandardOutput;
				if (readerContent != null) {
					while ((text = readerContent.ReadLine ()) != null) 
					{
						text = text.Remove (0, packageName.Length + 1);
						try
						{
							info.PackageContent.Add(text);
						}
						catch(Exception){}
					}
				}
				pacmanContentProcess.WaitForExit ();
			}
			pacmanContentProcess.Close ();
			
			if(OnPackageInformationFinished != null)
			{
				OnPackageInformationFinished(info);
			}
			#endregion
		}
		#endregion
		#endregion
	}
	
	public class packageInformation
	{
		public string Name;
		public string Description;
		public string version;
		public string URL;
		public string Packager;
		public string InstallationDate;
		public List<string> PackageContent = new List<string>();
		public Gtk.Image screenshot = new Gtk.Image();
	}
}

