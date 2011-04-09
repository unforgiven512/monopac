using System;
using System.Collections.Generic;
using System.IO;

namespace pacmanSharp
{
	public class PacmanSharp
	{
		public List<Repository> repositories { get; set; }

		public PacmanSharp ()
		{
			repositories = new List<Repository> ();
		}
		
		/// <summary>
		/// Reading repositories from /etc/pacman.conf
		/// </summary>
		/// <returns>
		/// true, if it was successful
		/// </returns>
		public bool readRepositories ()
		{
			bool ret = false;
			
			//delete the old repositories if there are any
			if (repositories.Count > 0) {
				repositories.Clear ();
			}
			
			string line = string.Empty;
			StreamReader file = null;
			try {
				file = new StreamReader ("/etc/pacman.conf");
				while ((line = file.ReadLine ()) != null) {
					
					//commented line
					if (line.StartsWith ("#"))
						continue;
					
					//empty line
					if (String.IsNullOrEmpty (line))
						continue;
					
					//options line
					if (line.Contains ("[options]"))
						continue;
					
					//repositories line
					if (line.StartsWith ("[")) {
						line = line.Replace ("[", "");
						line = line.Replace ("]", "");
						
						Repository rep = new Repository ();
						rep.Name = line;
						repositories.Add (rep);
					}
				}
				
				if (repositories.Count > 0)
					ret = true;

			} catch (Exception) {
				ret = false;
				repositories.Clear ();
			}
			
			if (file != null)
				file.Close ();
			file = null;
			
			return ret;
		}
		
		public bool readPackagesFromRepository (string repoName)
		{
			bool ret = false;
			
			if (String.IsNullOrEmpty (repoName))
				return false;
			
			foreach (Repository item in repositories) {
				if (item.Name.Equals (repoName)) {
					ret = item.readPackagesFromRepository ();
					break;
				}
			}
			
			return ret;
		}
	}
}