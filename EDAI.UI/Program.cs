using System.Windows;
using Velopack;

namespace EDAI.UI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if !DEBUG
        VelopackApp.Build().Run();
#endif
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
