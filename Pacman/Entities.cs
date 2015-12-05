using System;
using System.Drawing;
using System.IO;

namespace Pacman
{
    public class Map
    {
        // The sprites for the different tiles
        private Image WALL_SPRITE;
        private Image PATH_SPRITE;

        private int width;
        private int height;

        // This only stores whether or not the tiles are passable, as there are only 2 types of tiles
        private Boolean[][] tiles;

        // Temporary constructor to load a testing map
        public Map()
        {
            tiles = new Boolean[][]{ new Boolean[]{true, true, true, true},
                new Boolean[]{true, false, true, false},
                new Boolean[]{true, true, true, true},
                new Boolean[]{false, false, false, true}};

            WALL_SPRITE = Image.FromFile(Program.BaseDirectory + "\\sprites\\wall.png");
            PATH_SPRITE = Image.FromFile(Program.BaseDirectory + "\\sprites\\path.png");

            width = 4;
            height = 4;
        }

        public Map(String FilePath)
        {
            // TODO read from file
        }

        public Boolean[][] Tiles()
        {
            return tiles;
        }

        public Boolean GetTile(int x, int y)
        {
            return tiles[x][y];
        }

        // Returns the sprite of a tile at the given position (assuming it's within bounds)
        public Image GetSprite(int x, int y)
        {
            if (tiles[x][y])
                return PATH_SPRITE;
            else
                return WALL_SPRITE;
        }

        public int GetWidth()
        {
            return width;
        }
        public int GetHeight()
        {
            return height;
        }

    }


    // Any entity must have 3 attributes:
    // - They must be either visible or invisible (onscreen or offscreen)
    // - They must be at a position (if they are invisible, this is ignored) - this position does NOT have to conform to a given square (it can be a decimal)
    // - They must have a sprite (note: this sprite could change if, for example, they change direction)
    public interface Entity
    {
        Boolean Visible();
        void ToggleVisibility();

        double GetSize();
        double GetX();
        double GetY();
        Boolean SetPosition(double x, double y);
        Boolean ChangePosition(double x, double y);

        Image GetSprite();
    }

    // Pacman is the player-controlled character.
    public class Pacman : Entity
    {
        private static double SIZE = 0.8;
        private static Image[] OpenImage;
        private static Image[] ClosedImage;

        private double x;
        private double y;

        // up=0, right=1, down=2, left=3
        private int direction;

        // Whether or not Pacman's mouth is currently open
        private Boolean isOpen = true;

        // The map is needed to make sure we don't move outside its bounds or into a wall
        private Map map;

        public Pacman(double x, double y, int direction, Map map)
        {
            this.x = x;
            this.y = y;
            this.direction = direction;
            this.map = map;
        }

        public double GetSize()
        {
            return SIZE;
        }

        // TODO make pacman invisible when dead
        public Boolean Visible()
        {
            return true;
        }
        public void ToggleVisibility()
        {
        }

        public double GetX()
        {
            return x;
        }
        public double GetY()
        {
            return y;
        }

        // Moves Pacman to the specified position.
        // Does nothing if:
        // - The specified position is not fully on a tile where Pacman can move
        // - The specified position is not fully within the bounds of the map
        // SetPosition will change Pacman's direction as well
        public Boolean SetPosition(double x, double y)
        {
            // Check to make sure that the resulting position would be within the map
            if(x < 0 || y < 0 || x > map.GetWidth() - Pacman.SIZE || y > map.GetHeight() - Pacman.SIZE) 
                return false;

            // Make sure the specified position isn't within a wall
            // Simplest way: Check the up to 4 tiles bounding the position (i.e. if Pacman is at (5, 4.5), we need to check (5,4) and (5,5),
            // however if Pacman is at (4.5,4.5), we need to check (4,4), (4,5), (5,4) and (5,5)).

            // Wall in the top-left
            if (!map.GetTile((int)Math.Floor(x),(int)Math.Floor(y))) return false;
            // Wall in the bottom-left
            else if (!map.GetTile((int)Math.Floor(x),(int)Math.Floor(y + Pacman.SIZE - 0.01))) return false;
            // Wall in the top-right
            else if (!map.GetTile((int)Math.Floor(x + Pacman.SIZE - 0.01),(int)Math.Floor(y))) return false;
            // Wall in the bottom-right
            else if (!map.GetTile((int)Math.Floor(x + Pacman.SIZE - 0.01),(int)Math.Floor(y + Pacman.SIZE - 0.01))) return false;

            if (x > this.x)
                direction = 1;
            else if (x < this.x)
                direction = 3;
            else if (y > this.y)
                direction = 2;
            else if (y < this.y)
                direction = 0;

            this.x = x;
            this.y = y;

            return true;
        }

        // Alternatives to SetPosition, shifts Pacman's position by the given values
        // - If the resulting position would not be fully on a tile where Pacman can move,
        //   Pacman will go as far as possible up to xDiff,yDiff
        // - If the resulting position is not fully within the bounds of the map, the same
        //   will occur.
        public Boolean ChangePosition(double xDiff, double yDiff)
        {
            // Check to see if the resulting position would be within the map
            // If it wouldn't be, change xDiff and yDiff such that it would be within the map
            if(this.x + xDiff + Pacman.SIZE > map.GetWidth()) xDiff = map.GetWidth() - this.x - Pacman.SIZE;
            else if (this.x + xDiff < 0) xDiff = -this.x;
            if (this.y + yDiff + Pacman.SIZE > map.GetHeight()) yDiff = map.GetHeight() - this.y - Pacman.SIZE;
            else if (this.y + yDiff < 0) yDiff = -this.y;

            // Check to see if the then resulting position would be inside a wall (completely or partially)
            // And move the resulting position to the closest position that isn't inside a wall
            double newX = this.x + xDiff;
            double newY = this.y + yDiff;
            // Impassable square somewhere, so decrease the x and y differences until it isn't any more
            while(!map.GetTile((int)newX, (int)newY) ||
                  !map.GetTile((int)(newX + Pacman.SIZE - 0.01), (int)newY) ||
                  !map.GetTile((int)newX, (int)(newY + Pacman.SIZE - 0.01)) ||
                  !map.GetTile((int)(newX + Pacman.SIZE - 0.01), (int)(newY + Pacman.SIZE - 0.01)))
            {
                if (xDiff > 0.01) xDiff -= 0.01;
                else if (xDiff > 0) xDiff = 0;
                else if (xDiff < -0.01) xDiff += 0.01;
                else if (xDiff < 0) xDiff = 0;

                if (yDiff > 0.01) yDiff -= 0.01;
                else if (yDiff > 0) yDiff = 0;
                else if (yDiff < -0.01) yDiff += 0.01;
                else if (yDiff < 0) yDiff = 0;

                newX = this.x + xDiff;
                newY = this.y + yDiff;
            }


            if (xDiff > 0)
                direction = 1;
            else if (xDiff < 0)
                direction = 3;
            else if (yDiff > 0)
                direction = 2;
            else if (yDiff < 0)
                direction = 0;

            this.x += xDiff;
            this.y += yDiff;

            return true;
        }

        public Image GetSprite()
        {
            return isOpen ? OpenImage[direction] : ClosedImage[direction];
        }

        // Loads the various Pacman sprites; should be called before a Pacman is created
        public static void LoadSprites()
        {
            OpenImage = new Image[4];
            OpenImage[0] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacupopen.png");
            OpenImage[1] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacrightopen.png");
            OpenImage[2] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacdownopen.png");
            OpenImage[3] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacleftopen.png");

            ClosedImage = new Image[4];
            ClosedImage[0] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacupclosed.png");
            ClosedImage[1] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacrightclosed.png");
            ClosedImage[2] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacdownclosed.png");
            ClosedImage[3] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacleftclosed.png");
        }

    }
}

