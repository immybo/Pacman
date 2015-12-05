using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pacman
{
    public partial class MainWindow : Form
    {
        private HashSet<Entity> entities;
        private Map map;

        private Pacman pacman;
        // The direction in which Pacman is currently set to move (-1 is stopped, 0 = up, goes clockwise)
        private int pacmanMovementDirection = -1;

        private Timer loopTimer;

        public MainWindow()
        {
            Pacman.LoadSprites();
            InitializeComponent();
            // Double buffering to remove flickering
            this.SetStyle(
                System.Windows.Forms.ControlStyles.UserPaint |
                System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer,
                true);

            // Set up key press handling
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(MainWindow_KeyPress);

            // Set up the game loop timer
            loopTimer = new Timer();
            loopTimer.Interval = Program.TICK_TIME;
            loopTimer.Tick += new EventHandler(UpdateGame);
            loopTimer.Start();

            // Set up the list of entities and the map
            map = new Map();
            entities = new HashSet<Entity>();
            entities.Add(pacman = new Pacman(0.1, 0.1, 1, map));

            // Make sure it draws everything every time the form is painted
            this.Paint += Draw;
        }

        private int tileWidth;
        private int tileHeight;

        // Draws the window, taking a set of entities that are present on screen and the map.
        public void Draw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // First, draw the map tiles
            tileWidth = (this.Width-32) / map.GetWidth();
            tileHeight = (this.Height-32) / map.GetHeight();
            for (int i = 0; i < map.GetWidth(); i++)
            {
                for (int j = 0; j < map.GetHeight(); j++)
                {
                    g.DrawImage(map.GetSprite(i, j), i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                }
            }

            // .. and then the entities
            foreach (Entity entity in entities)
            {
                g.DrawImage(entity.GetSprite(), (int)(entity.GetX()*tileWidth), (int)(entity.GetY()*tileHeight), (int)(tileWidth*entity.GetSize()), (int)(tileHeight*entity.GetSize()));
            }
        }

        // Handles any key presses
        public void MainWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            DebugBox.Text = "Key " + e.KeyChar + " pressed.";

            if (e.KeyChar.Equals('w')) pacmanMovementDirection = 0;
            else if (e.KeyChar.Equals('a')) pacmanMovementDirection = 3;
            else if (e.KeyChar.Equals('s')) pacmanMovementDirection = 2;
            else if (e.KeyChar.Equals('d')) pacmanMovementDirection = 1;
        }
        
        // Invalidates and updates the window
        private void Redraw() { Invalidate(); Update(); }

        // Updates the game every tick
        private void UpdateGame(object sender, EventArgs e)
        {
            if (pacmanMovementDirection == 0) pacman.ChangePosition(0, -Program.MOVE_DIST);
            else if (pacmanMovementDirection == 1) pacman.ChangePosition(Program.MOVE_DIST, 0);
            else if (pacmanMovementDirection == 2) pacman.ChangePosition(0, Program.MOVE_DIST);
            else if (pacmanMovementDirection == 3) pacman.ChangePosition(-Program.MOVE_DIST, 0);

            Redraw();
        }
    }
}
