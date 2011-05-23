using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace LCGoogleApps
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new frmMain());
		}

		public static string ExecuteShellCmd(string FilePath, string Arguments)
		{
			FileInfo fi = new FileInfo(FilePath);

			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.FileName = FilePath;
			process.StartInfo.Arguments = Arguments;
			process.StartInfo.WorkingDirectory = fi.DirectoryName;
			process.Start();

			return process.StandardOutput.ReadToEnd();
		}
	}
}
