using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace pacmanSharp
{
	public class Package
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public List<string> Files { get; set; }
		public string URL { get; set; }
		public string License { get; set; }
		public string Groups { get; set; }
		public string Provides { get; set; }
		public List<string> DependsOn { get; set; }
		public List<string> OptionalDepends { get; set; }
		public List<string> RequiredBy { get; set; }
		public string ConflictsWith { get; set; }
		public string Replaces { get; set; }
		public string InstalledSize { get; set; }
		public string Packager { get; set; }
		public string Architecture { get; set; }
		public DateTime BuildDate { get; set; }
		public DateTime InstalledDate { get; set; }
		public string InstallReason { get; set; }
		public bool InstallScript { get; set; }
		public string Description { get; set; }
		
		public bool Installed { get; set; }
				
		public Package ()
		{
		}
		
		public bool readPackageInformation ()
		{
			bool ret = false;
			
			Process pacmanInfoProcess = new Process ();
			ProcessStartInfo startInfo = new ProcessStartInfo ("pacman", "-Qi " + Name);
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
						int firstColon = text.IndexOf (':');
						if (firstColon > 0) {
							string lineText = text.Remove (0, firstColon + 2);
							
							/*
							if (text.Contains ("Name")) {
								Name = lineText;
							} else if (text.Contains ("Version")) {
								Version = lineText;
							} else if (text.Contains ("URL")) {
								URL = lineText;
							} else if (text.Contains ("Description")) {
								Description = lineText;
							} else if (text.Contains ("Packager")) {
								Packager = lineText;
							} else if (text.Contains ("Install Date")) {
								InstallationDate = lineText;
							}*/
						}
					}
				}
				pacmanInfoProcess.WaitForExit ();
			}
			pacmanInfoProcess.Close ();
			
			
			return ret;
			
		}
	}
}

