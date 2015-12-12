using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Pacman
{
    public class Map
    {
        // The sprites for the different tiles
        private Image WALL_SPRITE = Image.FromFile(Program.BaseDirectory + "\\sprites\\wall.png");
        private Image PATH_SPRITE = Image.FromFile(Program.BaseDirectory + "\\sprites\\path.png");

        private int width;
        private int height;

        // 0 = wall
        // 1 = open ground
        private int[][] tiles;

        private HashSet<Entity> entities;

        // Temporary constructor to load a testing map
        public Map()
        {
            tiles = new int[][]{ new int[]{1,1,1,1},
                new int[]{1,0,1,0},
                new int[]{1,1,1,1},
                new int[]{0,0,0,1}};

            width = 4;
            height = 4;
        }

        public Map(String mapName)
        {
            try {
                using (StreamReader mapStream = new StreamReader(Program.BaseDirectory + "\\maps\\" + mapName + ".txt"))
                {
                    // Figure out map parameters (i.e. size)
                    String parameters = mapStream.ReadLine();
                    width = int.Parse(parameters.Split(new char[] { ' ' })[0]);
                    height = int.Parse(parameters.Split(new char[] { ' ' })[1]);

                    tiles = new int[width][];
                    for(int i = 0; i < width; i++) tiles[i] = new int[height];

                    // Go through the map line by line
                    for(int i = 0; i < height; i++)
                    {
                        String[] nextLine = mapStream.ReadLine().Split(new char[] { ' ' });
                        for(int j = 0; j < width; j++)
                        {
                            tiles[j][i] = int.Parse(nextLine[j]);
                        }
                    }

                    // And get the entities as well
                    entities = new HashSet<Entity>();
                    while (!mapStream.EndOfStream)
                    {
                        String[] nextLine = mapStream.ReadLine().Split(new char[] { ' ' });
                        double x = double.Parse(nextLine[1]);
                        double y = double.Parse(nextLine[2]);
                        switch (nextLine[0])
                        {
                            case "pellet":
                                entities.Add(new Pellet(x, y));
                                break;
                            case "ghost":
                                entities.Add(new Ghost(x, y, this));
                                break;
                            case "pacman":
                                entities.Add(new Pacman(x, y, 0, this));
                                break;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Could not properly read from map file '" + mapName + "'. ");
                Console.WriteLine(e.Message);
            }
        }

        public int[][] Tiles()
        {
            return tiles;
        }

        public HashSet<Entity> Entities()
        {
            return entities;
        }

        public int GetTile(int x, int y)
        {
            return tiles[x][y];
        }

        // Resets any tile to open ground (for removing pellets)
        public void EmptyTile(int x, int y)
        {
            tiles[x][y] = 1;
        }

        // Returns the sprite of a tile at the given position (assuming it's within bounds)
        public Image GetSprite(int x, int y)
        {
            if (tiles[x][y] == 0)
                return WALL_SPRITE;
            else if (tiles[x][y] == 1)
                return PATH_SPRITE;
            // Assume it's a wall if nothing else, just in case a map is made wrong
            else
            {
                tiles[x][y] = 0;
                return WALL_SPRITE;
            }
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

        // How large the entity is compared to a tile (0-1 typical)
        double GetSize();
        double GetX();
        double GetY();
        Boolean SetPosition(double x, double y);
        Boolean ChangePosition(double x, double y);

        Image GetSprite();
    }

    // Ghosts autonomously roam around the map and try to kill Pacman
    public class Ghost : Entity
    {
        // How many tiles a ghost will move per tick
        private static double SPEED = 0.1;
        private static double SIZE = 1;
        private static Image ghostImage;

        private double x;
        private double y;

        private double pacX = 0;
        private double pacY = 0;

        private Boolean visible = true;

        // up=0, right=1, down=2, left=3
        private int direction;

        private Map map;

        public Ghost(double x, double y, Map map)
        {
            this.x = x;
            this.y = y;
            this.map = map;
        }

        // Returns whether or not this ghost would be able to move to the specified position
        public Boolean CouldMoveTo(double x, double y)
        {
            // Check to make sure that the resulting position would be within the map
            if (x < 0 || y < 0 || x > map.GetWidth() - Ghost.SIZE || y > map.GetHeight() - Ghost.SIZE)
                return false;

            // Make sure the specified position isn't within a wall
            // Simplest way: Check the up to 4 tiles bounding the position

            // Wall in the top-left
            if (map.GetTile((int)Math.Floor(x), (int)Math.Floor(y)) == 0) return false;
            // Wall in the bottom-left
            else if (map.GetTile((int)Math.Floor(x), (int)Math.Floor(y + Ghost.SIZE - 0.01)) == 0) return false;
            // Wall in the top-right
            else if (map.GetTile((int)Math.Floor(x + Ghost.SIZE - 0.01), (int)Math.Floor(y)) == 0) return false;
            // Wall in the bottom-right
            else if (map.GetTile((int)Math.Floor(x + Ghost.SIZE - 0.01), (int)Math.Floor(y + Ghost.SIZE - 0.01)) == 0) return false;

            // Otherwise it can move
            return true;
        }

        // Moves this ghost to the specified position.
        // Does nothing if:
        // - The specified position is not fully on a tile where a ghost can move
        // - The specified position is not fully within the bounds of the map
        // SetPosition will change the ghost's direction as well
        public Boolean SetPosition(double x, double y)
        {
            if (!CouldMoveTo(x, y)) return false;

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

        // Alternatives to SetPosition, shifts the ghost's position by the given values
        // - If the resulting position would not be fully on a tile where the ghost can move,
        //   the ghost will go as far as possible up to xDiff,yDiff
        // - If the resulting position is not fully within the bounds of the map, the same
        //   will occur.
        public Boolean ChangePosition(double xDiff, double yDiff)
        {
            // Whether or not the ghost could be moved the entire distance
            bool returnValue = true;

            // Check to see if the resulting position would be within the map
            // If it wouldn't be, change xDiff and yDiff such that it would be within the map
            if (this.x + xDiff + Ghost.SIZE > map.GetWidth()) { xDiff = map.GetWidth() - this.x - Ghost.SIZE; returnValue = false; }
            else if (this.x + xDiff < 0) { xDiff = -this.x; returnValue = false; }
            if (this.y + yDiff + Ghost.SIZE > map.GetHeight()) { yDiff = map.GetHeight() - this.y - Ghost.SIZE; returnValue = false; }
            else if (this.y + yDiff < 0) { yDiff = -this.y; returnValue = false; }

            // Check to see if the then resulting position would be inside a wall (completely or partially)
            // And move the resulting position to the closest position that isn't inside a wall
            double newX = this.x + xDiff;
            double newY = this.y + yDiff;
            // Impassable square somewhere, so decrease the x and y differences until it isn't any more
            while (map.GetTile((int)newX, (int)newY) == 0 ||
                  map.GetTile((int)(newX + Ghost.SIZE - 0.01), (int)newY) == 0 ||
                  map.GetTile((int)newX, (int)(newY + Ghost.SIZE - 0.01)) == 0 ||
                  map.GetTile((int)(newX + Ghost.SIZE - 0.01), (int)(newY + Ghost.SIZE - 0.01)) == 0)
            {
                returnValue = false;

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

            return returnValue;
        }

        public static void LoadSprites()
        {
            ghostImage = Image.FromFile(Program.BaseDirectory + "\\sprites\\ghost1.png");
        }

        public double GetX()
        {
            return x;
        }
        public double GetY()
        {
            return y;
        }
        public double GetSize()
        {
            return SIZE;
        }
        public Image GetSprite()
        {
            return ghostImage;
        }
        public int GetDirection()
        {
            return direction;
        }

        // Unsupported
        public void ToggleVisibility() { }
        public Boolean Visible() { return visible; }

        // Returns whether or not this ghost is colliding with the given Pacman
        public bool IsColliding(Pacman pacman)
        {
            // In order to collide, the middle of Pacman must be within (Pacman's size + pellet's size) distance of the middle of the ghost (using roughly circular collision)
            double xDiff = (pacman.GetX() + pacman.GetSize() / 2) - (GetX() + GetSize() / 2);
            double yDiff = (pacman.GetY() + pacman.GetSize() / 2) - (GetY() + GetSize() / 2);
            double radius = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
            return radius < pacman.GetSize() / 2 + GetSize() / 2;
        }
        
        // Updates the ghost's last known Pacman position
        public void UpdatePacmanPosition(double x, double y)
        {
            pacX = x;
            pacY = y;
        }

        // Moves the ghost one tick according to its AI
        public void Move()
        {
            // Turnary within a turnary! What fun (easiest way to calculate xDiff and yDiff this tick based on direction)
            double xDiff = direction == 1 ? SPEED : (direction == 3 ? -SPEED : 0);
            double yDiff = direction == 2 ? SPEED : (direction == 0 ? -SPEED : 0);

            bool hitWall = !ChangePosition(xDiff, yDiff);

            // Figure out what directions we could move in
            HashSet<int> canTurnTo = new HashSet<int>();

            // Check if we COULD move in each direction
            if (CouldMoveTo(x, y - SPEED)) canTurnTo.Add(0);
            if (CouldMoveTo(x + SPEED, y)) canTurnTo.Add(1);
            if (CouldMoveTo(x, y + SPEED)) canTurnTo.Add(2);
            if (CouldMoveTo(x - SPEED, y)) canTurnTo.Add(3);

            // The other two directions will be +1/-1 if we're going in direction 1 or 2,
            // But if we're going in direction 0, they'll be 1/3, and if we're going in direction 3, they'll be 0/2
            int sideDirection1;
            int sideDirection2;
            if (direction == 0)
            {
                sideDirection1 = 3;
                sideDirection2 = 1;
            }
            else if (direction == 3)
            {
                sideDirection1 = 2;
                sideDirection2 = 0;
            }
            else
            {
                sideDirection1 = direction - 1;
                sideDirection2 = direction + 1;
            }

            // And also find the opposite direction
            int oppositeDirection;
            if (direction == 2 || direction == 3) oppositeDirection = direction - 2;
            else oppositeDirection = direction + 2;

            // If we've hit a wall, then we can't keep going straight, but we should consider turning around.
            // If we haven't hit a wall, the opposite is true.
            if (hitWall)
            {
                canTurnTo.Remove(direction);
                canTurnTo.Add(oppositeDirection);
            }
            else
            {
                canTurnTo.Add(direction);
                canTurnTo.Remove(oppositeDirection);
            }

            // If we can't move any direction except one, turn to that direction
            if (canTurnTo.Count == 1 || (canTurnTo.Count == 2 && canTurnTo.Contains(oppositeDirection)))
            {
                if (canTurnTo.Count == 2) canTurnTo.Remove(oppositeDirection);
                foreach (int i in canTurnTo)
                {
                    direction = i;
                    return;
                }
            }

            // If we can move in more than one direction, we don't want to turn 180 degrees
            canTurnTo.Remove(oppositeDirection);

            // But we do want to have some logic behind which direction we turn in 
            if (canTurnTo.Count != 1)
            {
                // There is a chance of just randomly choosing the direction, to make for unpredictable gameplay
                Random rng = new Random();
                if (rng.Next(2) == 0) // 50% chance
                {
                    // Randomly choose the direction
                    int number = rng.Next(canTurnTo.Count);
                    int it = 0;
                    foreach(int i in canTurnTo)
                    {
                        if(it == number)
                        {
                            direction = i;
                            return;
                        }
                        it++;
                    }
                }

                // But also some chance of moving towards whichever direction brings it closest to Pacman
                else
                {
                    // Otherwise figure out which direction would bring it closer to Pacman and go there
                    // Just uses literal distance rather than pathing
                    double[] dirNewX = { x, x + SPEED, x, x - SPEED };
                    double[] dirNewY = { y - SPEED, y, y + SPEED, y };

                    double[] distances = { double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue };
                    
                    foreach(int i in canTurnTo)
                    {
                        distances[i] = Math.Sqrt(Math.Pow(dirNewX[i] - pacX, 2) + Math.Pow(dirNewY[i] - pacY, 2));
                    }

                    int minPlace = -1;
                    double minDist = double.MaxValue;
                    for(int i = 0; i < distances.Length; i++)
                    {
                        if (distances[i] < minDist)
                        {
                            minDist = distances[i];
                            minPlace = i;
                        }
                    }

                    direction = minPlace;
                }
            }
        }
    }

    // Pellets are eaten by Pacman when he moves over them, to give score
    public class Pellet : Entity
    {
        private static double SIZE = 0.2;
        private static Image PelletImage;

        private double x;
        private double y;

        private Boolean visible = true;

        public Pellet(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public Boolean SetPosition(double x, double y)
        {
            this.x = x;
            this.y = y;
            // Pellets can always move to any position, and this should , in fact, only be called at the start of a map
            return true;
        }
        // Unsupported
        public Boolean ChangePosition(double x, double y)
        {
            return false;
        }

        public static void LoadSprite()
        {
            PelletImage = Image.FromFile(Program.BaseDirectory + "\\sprites\\pellet.png");
        }

        public double GetX()
        {
            return x;
        }
        public double GetY()
        {
            return y;
        }
        public double GetSize()
        {
            return SIZE;
        }
        public Image GetSprite()
        {
            return PelletImage;
        }
        
        // Unsupported
        public void ToggleVisibility() { }
        public Boolean Visible() { return visible; }

        // Returns whether or not this pellet is colliding with the given Pacman
        public bool IsColliding(Pacman pacman)
        {
            // In order to collide, the middle of Pacman must be within (Pacman's size + pellet's size) distance of the middle of the pellet
            double xDiff = (pacman.GetX() + pacman.GetSize() / 2) - (GetX() + GetSize() / 2);
            double yDiff = (pacman.GetY() + pacman.GetSize() / 2) - (GetY() + GetSize() / 2);
            double radius = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
            return radius < pacman.GetSize()/2 + GetSize()/2;
        }
    }

    // Pacman is the player-controlled character.
    public class Pacman : Entity
    {
        private static double SIZE = 0.8;
        public static int DEATH_FRAMES = 6;
        // Note that there are actually double this amount of frames, as this is the amount of frames in one of opening OR closing Pacman's mouth
        // (they are simply reversed from one another)
        private static int FRAMES = 3;

        public int deathFrame = -1;

        // [frame][direction]
        private static Image[][] images;
        private static Image[][] deathImages;

        private double x;
        private double y;

        // up=0, right=1, down=2, left=3
        private int direction;

        // The current frame of Pacman's mouth opening and shutting
        private int frame = 0;

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
        public int GetDirection()
        {
            return direction;
        }
        public void SetDirection(int direction)
        {
            this.direction = direction;
        }

        // Moves Pacman to the specified position.
        // Does nothing if:
        // - The specified position is not fully on a tile where Pacman can move
        // - The specified position is not fully within the bounds of the map
        // SetPosition will not change Pacman's direction
        public Boolean SetPosition(double x, double y)
        {
            // Check to make sure that the resulting position would be within the map
            if (x < 0 || y < 0 || x > map.GetWidth() - Pacman.SIZE || y > map.GetHeight() - Pacman.SIZE)
                return false;

            // Make sure the specified position isn't within a wall
            // Simplest way: Check the up to 4 tiles bounding the position (i.e. if Pacman is at (5, 4.5), we need to check (5,4) and (5,5),
            // however if Pacman is at (4.5,4.5), we need to check (4,4), (4,5), (5,4) and (5,5)).

            // Wall in the top-left
            if (map.GetTile((int)Math.Floor(x), (int)Math.Floor(y)) == 0) return false;
            // Wall in the bottom-left
            else if (map.GetTile((int)Math.Floor(x), (int)Math.Floor(y + Pacman.SIZE - 0.01)) == 0) return false;
            // Wall in the top-right
            else if (map.GetTile((int)Math.Floor(x + Pacman.SIZE - 0.01), (int)Math.Floor(y)) == 0) return false;
            // Wall in the bottom-right
            else if (map.GetTile((int)Math.Floor(x + Pacman.SIZE - 0.01), (int)Math.Floor(y + Pacman.SIZE - 0.01)) == 0) return false;

            this.x = x;
            this.y = y;

            return true;
        }

        // Alternatives to SetPosition, shifts Pacman's position by the given values
        // - If the resulting position would not be fully on a tile where Pacman can move,
        //   Pacman will go as far as possible up to xDiff,yDiff
        // - If the resulting position is not fully within the bounds of the map, the same
        //   will occur.
        // ChangePosition will not change Pacman's direction
        public Boolean ChangePosition(double xDiff, double yDiff)
        {
            // Check to see if the resulting position would be within the map
            // If it wouldn't be, change xDiff and yDiff such that it would be within the map
            if (this.x + xDiff + Pacman.SIZE > map.GetWidth()) xDiff = map.GetWidth() - this.x - Pacman.SIZE;
            else if (this.x + xDiff < 0) xDiff = -this.x;
            if (this.y + yDiff + Pacman.SIZE > map.GetHeight()) yDiff = map.GetHeight() - this.y - Pacman.SIZE;
            else if (this.y + yDiff < 0) yDiff = -this.y;

            // Check to see if the then resulting position would be inside a wall (completely or partially)
            // And move the resulting position to the closest position that isn't inside a wall
            double newX = this.x + xDiff;
            double newY = this.y + yDiff;
            // Impassable square somewhere, so decrease the x and y differences until it isn't any more
            while (map.GetTile((int)newX, (int)newY) == 0 ||
                  map.GetTile((int)(newX + Pacman.SIZE - 0.01), (int)newY) == 0 ||
                  map.GetTile((int)newX, (int)(newY + Pacman.SIZE - 0.01)) == 0 ||
                  map.GetTile((int)(newX + Pacman.SIZE - 0.01), (int)(newY + Pacman.SIZE - 0.01)) == 0)
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

            this.x += xDiff;
            this.y += yDiff;

            return true;
        }

        public Image GetSprite()
        {
            // If Pacman is dying, return his current death frame, otherwise return a regular frame
            if(deathFrame != -1)
            {
                return deathImages[deathFrame][direction];
            }

            // If Pacman's mouth is opening, this is obvious
            else if (frame < FRAMES)
            {
                return images[frame][direction];
            }
            // If Pacman's mouth is closing, however, we have to count back down from the last frame
            else if(frame < FRAMES*2)
            {
                return images[FRAMES - (2 * FRAMES - frame)][direction];
            }
            // If we're not within bounds, catch it by sticking to the initial frame
            else
            {
                return images[0][direction];
            }
        }

        // Loads the various Pacman sprites; should be called before a Pacman is created
        public static void LoadSprites()
        {
            images = new Image[FRAMES][];
            for (int i = 0; i < FRAMES; i++)
            {
                images[i] = new Image[4];

                // Facing up: Rotate 90 degrees counterclockwise and flip horizontally
                images[i][0] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pac" + i + ".png");
                images[i][0].RotateFlip(RotateFlipType.Rotate270FlipX);
                // Facing right: No transform
                images[i][1] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pac" + i + ".png");
                // Facing down: Rotate 90 degrees clockwise
                images[i][2] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pac" + i + ".png");
                images[i][2].RotateFlip(RotateFlipType.Rotate90FlipNone);
                // Facing left: Flip horizontally
                images[i][3] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pac" + i + ".png");
                images[i][3].RotateFlip(RotateFlipType.RotateNoneFlipX);
            }

            deathImages = new Image[DEATH_FRAMES][];
            for (int i = 0; i < DEATH_FRAMES; i++)
            {
                deathImages[i] = new Image[4];

                // Identical conversions to regular sprites

                deathImages[i][0] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacdeath" + i + ".png");
                deathImages[i][0].RotateFlip(RotateFlipType.Rotate270FlipX);

                deathImages[i][1] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacdeath" + i + ".png");

                deathImages[i][2] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacdeath" + i + ".png");
                deathImages[i][2].RotateFlip(RotateFlipType.Rotate90FlipNone);

                deathImages[i][3] = Image.FromFile(Program.BaseDirectory + "\\sprites\\pacdeath" + i + ".png");
                deathImages[i][3].RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
        }

        // Goes to the next frame of Pacman's mouth being open or shut
        public void NextFrame()
        {
            if(frame < FRAMES*2){
                frame++;
            }
            else{
                frame = 0;
            }
        }

        // Signifies the end of Pacman's death animation
        public void EndDeath()
        {
            deathFrame = -1;
        }
        // Goes to Pacman's next death frame - starts his death animation if it has not yet started
        public void NextDeathFrame()
        {
            deathFrame++;
        }
    }
}

