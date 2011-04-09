using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace pacmanSharp
{
	public class Repository
	{
		public string Name { get; set; }
		public List<Package> Packages { get; set; }
		public int NumberOfInstalledPackages {
			get 
			{
				int count = 0;
				foreach (Package item in Packages) {
					if (item.Installed) {
						count++;
					}
				}
				return count;
			}
		}
		
		public Repository ()
		{
			Packages = new List<Package> ();
		}
		
		public bool readPackagesFromRepository ()
		{
			bool ret = false;
			
			Process pacmanInfoProcess = new Process ();
			ProcessStartInfo startInfo = new ProcessStartInfo ("/usr/bin/pacman", "-Sl " + Name);
			startInfo.EnvironmentVariables.Remove ("LC_ALL");
			//delete old LC_ALL for this process...
			startInfo.EnvironmentVariables.Add ("LC_ALL", "C");
			//and set the standard to get english output
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			pacmanInfoProcess.StartInfo = startInfo;
			ret = pacmanInfoProcess.Start ();
			
			string text = String.Empty;
			StreamReader reader;
			
			if (ret) {
				reader = pacmanInfoProcess.StandardOutput;
				if (reader != null) {
					while ((text = reader.ReadLine ()) != null) {
						//Remove the reponame from the line
						text = text.Remove (0, Name.Length).Trim ();
						string[] packageInfo = text.Split (' ');
						
						Package package = new Package ();
						package.Name = packageInfo[0];
						package.Version = packageInfo[1];
						
						if (packageInfo.Length > 2)
							package.Installed = true;
						
						Packages.Add (package);
						
						ret = true;
					}
				}
				pacmanInfoProcess.WaitForExit ();
			}
			pacmanInfoProcess.Close ();
			
			return ret;
		}
		
		public List<Package> getInstalledPackages ()
		{
			List<Package> installedPackages = new List<Package> ();
			
			foreach (Package item in Packages) {
				if (item.Installed) {
					installedPackages.Add (item);
				}
			}
			
			return installedPackages;
		}
	}
}

