using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Pacman
{
    static class Program
    {
        // The amount Pacman will move per key press
        public static double MOVE_DIST = 0.1;
        // The tick time in ms
        public static int TICK_TIME = 50;
        // The amount of points a pellet gives
        public static int PELLET_SCORE = 10;

        public static DirectoryInfo BaseDirectory = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString());

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
