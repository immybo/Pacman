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
        private Map map;

        private Pacman pacman;
        private HashSet<Pellet> pellets;
        private HashSet<Ghost> ghosts;

        private Timer loopTimer;

        private int score;
        private int lives;

        public MainWindow()
        {
            Pacman.LoadSprites();
            Pellet.LoadSprite();
            Ghost.LoadSprites();

            InitializeComponent();

            // Set up the GUI
            score = 0;
            lives = 3;

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

            pellets = new HashSet<Pellet>();
            ghosts = new HashSet<Ghost>();

            // Set up the list of entities and the map
            map = new Map("level1");
            HashSet<Entity> entities = map.Entities();
            foreach (Entity e in entities)
            {
                if (e is Pacman)
                    pacman = (Pacman)e;
                else if (e is Pellet)
                    pellets.Add((Pellet)e);
                else if (e is Ghost)
                    ghosts.Add((Ghost)e);
            }

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
            foreach (Pellet p in pellets)
                DrawEntity(g, p);
            foreach(Ghost gh in ghosts)
                DrawEntity(g, gh);

            DrawEntity(g, pacman);
        }

        // Draws the given entity at the appropriate position
        private void DrawEntity(Graphics g, Entity e)
        {
            g.DrawImage(e.GetSprite(), (int)(e.GetX() * tileWidth), (int)(e.GetY() * tileHeight), (int)(tileWidth * e.GetSize()), (int)(tileHeight * e.GetSize()));
        }

        // Handles any key presses
        public void MainWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals('w')) pacman.SetDirection(0);
            else if (e.KeyChar.Equals('a')) pacman.SetDirection(3);
            else if (e.KeyChar.Equals('s')) pacman.SetDirection(2);
            else if (e.KeyChar.Equals('d')) pacman.SetDirection(1);
        }
        
        // Invalidates and updates the window
        private void Redraw() { Invalidate(); Update(); }

        // Updates the game every tick
        private void UpdateGame(object sender, EventArgs e)
        {
            DebugBox.Text = "Score: " + score + " || Lives : " + lives;

            MovePacman();
            MoveGhosts();

            CheckPelletCollision();
            CheckGhostCollision();
            Redraw();
        }

        // Moves Pacman in the direction where he would go
        private void MovePacman()
        {
            if (pacman.GetDirection() == 0) pacman.ChangePosition(0, -Program.MOVE_DIST);
            else if (pacman.GetDirection() == 1) pacman.ChangePosition(Program.MOVE_DIST, 0);
            else if (pacman.GetDirection() == 2) pacman.ChangePosition(0, Program.MOVE_DIST);
            else if (pacman.GetDirection() == 3) pacman.ChangePosition(-Program.MOVE_DIST, 0);

            pacman.NextFrame();
        }
        
        // Checks to see if Pacman is colliding with a pellet (and takes the appropriate actions if so)
        private void CheckPelletCollision()
        {
            HashSet<Pellet> toRemove = new HashSet<Pellet>();
            foreach(Pellet p in pellets)
            {
                if (p.IsColliding(pacman))
                {
                    toRemove.Add(p);
                    score += Program.PELLET_SCORE;
                }
            }

            foreach(Pellet p in toRemove)
            {
                pellets.Remove(p);
            }
        }

        // Manages the ghosts' AIs for another tick
        private void MoveGhosts()
        {
            foreach(Ghost g in ghosts)
            {
                g.UpdatePacmanPosition(pacman.GetX(), pacman.GetY());
                g.Move();
            }
        }

        // Checks to see if Pacman is colliding with a ghost (and takes the appropriate action if so)
        private void CheckGhostCollision()
        {
            foreach(Ghost g in ghosts)
            {
                if (g.IsColliding(pacman))
                {
                    PauseGame();
                    StartPacmanDeath();
                }
            }
        }

        // Resets the level to its initial position,
        // moving all entities to their original positions.
        private void ResetLevel()
        {
            pellets = new HashSet<Pellet>();
            pellets.Add(new Pellet(3, 3));
            ghosts = new HashSet<Ghost>();
            ghosts.Add(new Ghost(2, 2, map));
            pacman = new Pacman(0.1, 0.1, 1, map);
        }

        // Starts playing Pacman's death animation, and processes the loss of life when done
        // (i.e. resets the level or finishes the game)
        private void StartPacmanDeath()
        {
            pacman.deathFrame = 0;
            Timer animationTimer = new Timer();
            animationTimer.Interval = Program.TICK_TIME;
            animationTimer.Tick += new EventHandler(NextDeathFrame);
            animationTimer.Start();
        }
        // Updates Pacman to the next frame of its death animation
        // Processes the loss of life when done
        private void NextDeathFrame(object sender, EventArgs e)
        {
            pacman.NextDeathFrame();
            if (pacman.deathFrame == Pacman.DEATH_FRAMES)
            {
                ((Timer)sender).Stop();
                pacman.EndDeath();
                ProcessDeath();
                return;
            }
            Redraw();
        }
        // Processes the death of Pacman
        private void ProcessDeath()
        {
            lives--;
            ResetLevel();
            ResumeGame();
        }

        // Pauses the game timer so that nothing can happen
        private void PauseGame()
        {
            loopTimer.Stop();
        }
        // Does nothing if the game was not previously paused
        // Resumes the game timer if it was
        private void ResumeGame()
        {
            loopTimer.Start();
        }
    }
}
