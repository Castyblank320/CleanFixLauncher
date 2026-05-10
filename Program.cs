using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace CleanFixLauncher;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Verificar si se está ejecutando como administrador
        if (!IsAdministrator())
        {
            // Si no, relanzar el programa con permisos elevados
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas"  // Esto pide elevación
                };
                Process.Start(psi);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo obtener permisos de administrador.\n{ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}