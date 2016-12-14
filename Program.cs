using System;
using System.Threading;
using System.Windows.Forms;

namespace Authentiqr.NET
{
    static class Program
    {
        static Mutex instanceMutex;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Make sure only one instance with the form is displayed
            bool firstInstance;

            using (instanceMutex = new System.Threading.Mutex(true, "{9369686F-9050-4353-8637-2190C5536FF7}", out firstInstance))
            {
                if (!firstInstance)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
            }
        }
    }
}
