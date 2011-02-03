//  
//  MainWindow.cs
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Gtk;

public partial class MainWindow : Gtk.Window
{
	pacinfo.pacmanWrapper pacmanWrapper = new pacinfo.pacmanWrapper ();

	Gtk.TreeViewColumn packageColumn = new Gtk.TreeViewColumn ();
	Gtk.CellRendererText packageCell = new Gtk.CellRendererText ();
	Gtk.TreeModelFilter filter;
	Gtk.ListStore packageListStore = new Gtk.ListStore (typeof(string));
	uint timer;

	public MainWindow () : base(Gtk.WindowType.Toplevel)
	{
		Build ();
		
		packageColumn.Title = global::Mono.Unix.Catalog.GetString ("Packages");
		packageColumn.PackStart (packageCell, true);
		tree.AppendColumn (packageColumn);
		packageColumn.AddAttribute (packageCell, "text", 0);
					
		pacmanWrapper.OnAllInstalledPackagesFinished += HandlePacmanWrapperOnAllInstalledPackagesFinished;
		pacmanWrapper.OnPackageInformationFinished += HandlePacmanWrapperOnPackageInformationFinished;
		
		pacmanWrapper.getAllInstalledPackagesAsync ();
		
		progressbar2.Visible = true;
		timer = GLib.Timeout.Add(100, new GLib.TimeoutHandler (progress_timeout) );
	}
	
	private bool progress_timeout()
	{
		 progressbar2.Pulse();
		//returning true to call this timeout method again
		return true;
	}


	void HandlePacmanWrapperOnPackageInformationFinished (pacinfo.packageInformation info)
	{
		Gtk.Application.Invoke (delegate {
			if (info != null) {
				textview1.Buffer.BeginUserAction ();
				textview1.Buffer.Text = String.Empty;
				textview1.Buffer.Clear ();
				
				if (info.screenshot != null) 
				{
					if (info.screenshot.Pixbuf != null) 
					{
						using (Gdk.Pixbuf oldPixBuf = image1.Pixbuf) 
						{
							image1.Pixbuf = info.screenshot.Pixbuf;
						}
					}
					else
					{
						image1.Pixbuf = info.screenshot.Pixbuf;
					}
				}
				
				nameLabelText.Text = info.Name;
				versionLabelText.Text = info.version;
				urlLabelText.Text = info.URL;
				descriptionLabelText.Text = info.Description;
				packagerLabelText.Text = info.Packager;
				installDateLabelText.Text = info.InstallationDate;
				
				string bufferText = String.Empty;
				foreach (string file in info.PackageContent) 
				{
					bufferText += file + "\n";
				}
				
				textview1.Buffer.Text = bufferText;
				textview1.Buffer.EndUserAction ();
			}					
			progressbar2.Visible = false;
			GLib.Source.Remove (timer);
		});
	}

	void HandlePacmanWrapperOnAllInstalledPackagesFinished (System.Collections.Generic.List<string> allPackages)
	{
		Gtk.Application.Invoke (delegate {
			foreach (string item in allPackages) 
			{
				packageListStore.AppendValues (item);
			}
						
			filter = new Gtk.TreeModelFilter (packageListStore, null);
			// Specify the function that determines which rows to filter out and which ones to display
			filter.VisibleFunc = new Gtk.TreeModelFilterVisibleFunc (FilterTree);
			
			// Assign the filter as our tree's model
			tree.Model = filter;
			
			progressbar2.Visible = false;
			GLib.Source.Remove (timer);
		});
	}

	/// <summary>
	/// Is fired if a package is selected in the tree view
	/// </summary>
	/// <param name="sender">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="e">
	/// A <see cref="EventArgs"/>
	/// </param>
//	void OnSelectionEntry (object sender, EventArgs e)
//	{
//		TreeModel model;
//		TreeIter iter;
//		
//		if (((TreeSelection)sender).GetSelected (out model, out iter)) {
//			string packageName = (string)model.GetValue (iter, 0);
//			pacmanWrapper.getInformationOfPackageAsync (packageName);
//		}
//	}

	private bool FilterTree (Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		string packageName = model.GetValue (iter, 0).ToString ();
		
		if (filterEntry.Text == "")
			return true;
		
		if (packageName.IndexOf (filterEntry.Text) > -1)
			return true;
		else
			return false;
	}


	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		a.RetVal = true;
		Application.Quit ();
	}
	protected virtual void OnQuitActionActivated (object sender, System.EventArgs e)
	{
		GLib.Source.Remove (timer);
		Application.Quit ();
	}

	/// <summary>
	/// Filter the tree view if text is typed into the entry box
	/// </summary>
	/// <param name="sender">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="e">
	/// A <see cref="System.EventArgs"/>
	/// </param>
	protected virtual void OnFilterEntryChanged (object sender, System.EventArgs e)
	{
		// Since the filter text changed, tell the filter to re-determine which rows to display
		if(filter != null)
			filter.Refilter ();
	}

	protected virtual void OnTreeCursorChanged (object sender, System.EventArgs e)
	{
		TreeSelection selection = (sender as TreeView).Selection;
		TreeModel model;
		TreeIter iter;
		
		if (selection.GetSelected (out model, out iter)) {
			GLib.Source.Remove (timer);
			progressbar2.Visible = true;
			timer = GLib.Timeout.Add (100, new GLib.TimeoutHandler (progress_timeout));
			string packageName = (string)model.GetValue (iter, 0);
			pacmanWrapper.getInformationOfPackageAsync (packageName);
		}
	}
	
	protected virtual void OnAboutActionActivated (object sender, System.EventArgs e)
	{
		// Create a new About dialog
		AboutDialog about = new AboutDialog ();
		
		// Change the Dialog's properties to the appropriate values.
		about.ProgramName = "pacinfo";
		about.Version = "0.1";
		about.Authors = new string[] { "Daniel Isenmann <daniel.isenmann@gmx.de>" };
		about.WrapLicense = true;
		about.License = "This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.\n\nThis program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.\n\nYou should have received a copy of the GNU General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.";
		
		// Show the Dialog and pass it control
		about.Run ();
		
		// Destroy the dialog
		about.Destroy ();

	}

	protected virtual void OnEventbox1ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
	{
		pacinfo.ImageWindow window = new pacinfo.ImageWindow ();
		window.Title = "pacinfo: " + nameLabelText.Text;
		window.Modal = true;
		
		try {
			HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create ("http://screenshots.debian.net/screenshot/" + nameLabelText.Text);
			HttpWebResponse response = (HttpWebResponse)myReq.GetResponse ();
			
			if (response.StatusCode == HttpStatusCode.OK) 
			{
				using (Gdk.Pixbuf oldPixBuf = window.getImage ().Pixbuf) 
				{
					window.setImage (new Gdk.Pixbuf (response.GetResponseStream ()));
				}
			}
		}
		catch (Exception) 
		{
		}
		
		window.Show ();
	}
}