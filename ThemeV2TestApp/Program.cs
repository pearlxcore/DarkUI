using System;
using System.Windows.Forms;

namespace ThemeV2TestApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ThemeV2LabForm());
    }
}
