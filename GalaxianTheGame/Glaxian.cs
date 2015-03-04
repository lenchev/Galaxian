using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

/*
 * G A L A X I A N - The Game 
 * 
 * What you're seeing is only the basic concept of the game.
 * The game is far from finished, although playable.
 * Enjoy!
 * 
 *  ToDo: add sounds, enemy ships fire, add boss (perhaps), add a fancy end game screen, high score.
 */

namespace GalaxianGame
{

    class Galaxian
    {
        // variable for the scren buffer
        static ScreenBuffer buffer;    

        // define a structure for the spaceships
        struct Spaceship
        {
            public int x, y;
            public int lives;
            public ConsoleColor color;
            public string spaceshipDesign;
        }

        // define a structure for the shells
        public struct Shell
        {
            public int x, y;
            public bool goingUp;
            public char shellDesign;
            public ConsoleColor color;
        }

        // define a structure for the enemy shells
        public struct enemyShell
        {
            public int x, y;
            public bool goingDown;
            public char enemyShellDesign;
            public ConsoleColor color;
        }

        // define the stars' structure
        struct Star
        {
            // coordiates
            public int x;
            public int y;

            // star symbol
            public char starDesign;
        }

        static int playerScore = 0;
        static int playerLives = 0;

        static void Main()
        {
            // console window settings
            short windowWidth = 80;
            short windowHeight = 58;

            // set the console window settings
            Console.Title = "Galaxian";
            Console.BufferHeight = Console.WindowHeight = windowHeight; // the size can be changed, but probably will break CoverPage
            Console.BufferWidth = Console.WindowWidth = windowWidth;   // the size can be changed, but probably will break CoverPage
            Console.CursorVisible = false;

            // game settings
            int levels = 2;
            int currentLevel = 1;
            char shellDesign = '*';
            char enemyShellDesign = '|';
            bool playerWon = false;
            // end of game settings

            // Initially starting page
            TitleScreen();
            MainPage();

            // Start game
            FaceTheEnemyMessage();

            // create the player spaceship
            Spaceship playerShip = CreatePlayerShip();

            int enemyShipsNumber = 5; // number of enemy ships on one line. Can be changed later
            List<Spaceship> enemyShips = new List<Spaceship>();
            string enemyDirection = "right"; // first direction
            bool moveLeft = false;

            CreateEnemyShips(enemyShipsNumber, enemyShips);

            // initialize screen buffer
            buffer = new ScreenBuffer(windowWidth, windowHeight);

            // initiate two lists for the stars
            List<Star> backgroundStars = GenerateStars(70);
            List<Star> movedStars = new List<Star>(70);

            // initialize a list for the shells
            List<Shell> shells = new List<Shell>();

            // initialize a list for the enemyShells
            List<enemyShell> enemyShells = new List<enemyShell>();

            // game cycle
            while (playerShip.lives > 0)
            {   
                // handle keypresses
                KeyDetector(shellDesign, ref playerShip, ref shells);

                // move enemy Ships
                // we still have enemies alive
                if (enemyShips.Count > 0 && currentLevel <= levels)
                {
                    MoveEnemies(ref enemyShips, ref moveLeft);
                }
                // we don't have any enemies left, but the game is far from finished
                else if(enemyShips.Count == 0 && currentLevel < levels)
                {
                    // increment the level and add enemies
                    currentLevel++;
                    CreateEnemyShips(enemyShipsNumber + currentLevel * 2, enemyShips);
                }
                // no more enemies and no more levels! We won!
                else if (enemyShips.Count == 0 && currentLevel == levels)
                {
                    playerWon = true;
                    break;
                }
                  
                // STARS !
                // move the stars
                movedStars = MoveStars(backgroundStars);
                // draw the new moved stars
                DrawStars(movedStars);
                // save the new stars into the old stars list
                backgroundStars.Clear();
                backgroundStars = new List<Star>(movedStars);
                movedStars.Clear();
                // end of STARS !

                // draw the scoreboard
                DrawScoreboard();

                // draw the player spaceship
                DrawSpaceship(playerShip);

                // draw the spaceship shells
                shells = UpdateShells(shells);
                DrawShells(shells);

                // draw the spaceship shells
                enemyShells = UpdateEnemyShells(enemyShells);
                DrawEnemyShells(enemyShells);

                //draw enemy ships
                foreach (var enemyShip in enemyShips)
                {
                    DrawSpaceship(enemyShip);
                }
             
                // draw the buffer/screen
                buffer.DrawBuffer();

                // slow down the game
                Thread.Sleep(50);

                // add score points to the player
                if (isPlayerShipHitEnemy(shells, enemyShips))
                {
                    playerScore++;
                }

                // not working - need more investigation
                if (CheckEveryEnemyShipBumpPlayer(enemyShips, playerShip))
                {
                    playerLives--;
                    break;
                }

                // clear the buffer/screen
                buffer.ClearBuffer();
            }

            // clear the console screen and display the results
            Console.Clear();
            if (playerWon)
            {
                Console.WriteLine("YOU WIN!");
                Console.WriteLine("Score: {0}", playerScore);
            }
            else
            {
                Console.WriteLine("LOSER!");
                Console.WriteLine("Score: {0}", playerScore);
            }

        }

        // gets the x coordinate of the most right ship
        private static int MostRightShip(List<Spaceship> enemyShips)
        {
            // mostRightShipCoord = x + shipWidth - 1;
            int shipWidth = enemyShips[0].spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length;
            int mostRightShipCoord = enemyShips[0].x + shipWidth - 1;

            for (int i = 0; i < enemyShips.Count; i++)
            {
                // get ship width
                shipWidth = enemyShips[i].spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length;

                // is the current ship.x bigger than the last one?
                if ((enemyShips[i].x + shipWidth - 1) > mostRightShipCoord)
                {
                    mostRightShipCoord = enemyShips[i].x + shipWidth - 1;
                }
            }

            // return the results
            return mostRightShipCoord;
        }

        // gets the y coordinate of the most left ship
        private static int MostLeftShip(List<Spaceship> enemyShips)
        {
            int mostLeftShipCoord = enemyShips[0].x;

            for (int i = 0; i < enemyShips.Count; i++)
            {
                if (enemyShips[i].x < mostLeftShipCoord)
                {
                    mostLeftShipCoord = enemyShips[i].x;
                }
            }

            return mostLeftShipCoord;
        }

        // Generate the player spaceship
        private static Spaceship CreatePlayerShip()
        {
            Spaceship playerShip = new Spaceship();

            // the design of the spaceship
            playerShip.spaceshipDesign = "   A   \n";
            playerShip.spaceshipDesign += "  / \\  \n";
            playerShip.spaceshipDesign += "O-HHH-O\n";
            playerShip.spaceshipDesign += "O HHH O\n";
            playerShip.spaceshipDesign += "O-\\_/-O";

            // set the x coordinate to the center of the screen
            // x = (window width / 2) - (a single line of the spaceship / 2)
            playerShip.x = (Console.WindowWidth / 2) - (playerShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length) / 2;
            // y = bottom line of the screen - the number of lines the ship is composed of
            playerShip.y = Console.WindowHeight - 3 - playerShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;

            // set the player ship color
            playerShip.color = ConsoleColor.White;

            // set the player lives
            playerShip.lives = 3;

            return playerShip;
        }

        // Generate enemies
        private static void CreateEnemyShips(int enemyShipsNumber, List<Spaceship> enemyShips)
        {
            Spaceship firstEnemyShip = new Spaceship();//the most top enemy ship
            firstEnemyShip.spaceshipDesign = " OO \n";
            firstEnemyShip.spaceshipDesign += "OOOO\n";
            firstEnemyShip.spaceshipDesign += "O  O";

            firstEnemyShip.x = 17 - (firstEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length) / 2;
            firstEnemyShip.y = 5 - firstEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            firstEnemyShip.color = ConsoleColor.Red;

            for (int i = 0; i < enemyShipsNumber; i++)
            {
                enemyShips.Add(firstEnemyShip);
                firstEnemyShip.x += 7;
            }


            Spaceship secondEnemyShip = new Spaceship();//second enemy ship
            secondEnemyShip.spaceshipDesign = " HH \n";
            secondEnemyShip.spaceshipDesign += "H  H\n";
            secondEnemyShip.spaceshipDesign += " HH ";

            secondEnemyShip.x = 17 - (secondEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length) / 2;
            secondEnemyShip.y = (firstEnemyShip.y + 7) - secondEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            secondEnemyShip.color = ConsoleColor.Blue;

            for (int i = 0; i < enemyShipsNumber; i++)
            {
                enemyShips.Add(secondEnemyShip);
                secondEnemyShip.x += 7;
            }

            Spaceship thirdEnemyShip = new Spaceship();//Third enemy ship
            thirdEnemyShip.spaceshipDesign = "X  X\n";
            thirdEnemyShip.spaceshipDesign += " XX \n";
            thirdEnemyShip.spaceshipDesign += "X  X";

            thirdEnemyShip.x = 17 - (thirdEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length) / 2;
            thirdEnemyShip.y = (secondEnemyShip.y + 7) - thirdEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            thirdEnemyShip.color = ConsoleColor.Green;

            for (int i = 0; i < enemyShipsNumber; i++)
            {
                enemyShips.Add(thirdEnemyShip);
                thirdEnemyShip.x += 7;
            }

            Spaceship fourthEnemyShip = new Spaceship();//Fourth enemy ship
            fourthEnemyShip.spaceshipDesign = "T  T\n";
            fourthEnemyShip.spaceshipDesign += "TTTT\n";
            fourthEnemyShip.spaceshipDesign += " TT \n";
            fourthEnemyShip.spaceshipDesign += "TTTT";

            fourthEnemyShip.x = 17 - (fourthEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length) / 2;
            fourthEnemyShip.y = (thirdEnemyShip.y + 8) - fourthEnemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            fourthEnemyShip.color = ConsoleColor.Yellow;

            for (int i = 0; i < enemyShipsNumber; i++)
            {
                enemyShips.Add(fourthEnemyShip);
                fourthEnemyShip.x += 7;
            }
        }

        // Display pre-start message
        private static void FaceTheEnemyMessage()
        {
            while (true)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        break;
                    }

                    string faceTheEnemy = @"..\..\FaceTheEnemy.txt";
                    PrintTxtFile(faceTheEnemy, 20, 40);

                    if (Console.KeyAvailable)
                    {
                        break;
                    }

                    Console.Clear();
                    Thread.Sleep(2000);
                    break;

                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("File not found!! Press any key to continue...");
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("No file path is given!! Press any key to continue...");
                }
            }
        }

        // Cheking for key press. If so continue with program. If not - printing title scren and scores in a loop.
        private static void TitleScreen()
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    break;
                }

                try
                {
                    string title = @"..\..\TitleScreen.txt";
                    PrintTxtFile(title, 1000, 120);

                    string highScore = @"..\..\HighScore.txt";
                    PrintTxtFile(highScore, 1000, 120);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("File not found!! Press any key to continue...");
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("No file path is given!! Press any key to continue...");
                }
            }

            Console.Clear();
        }

        private static void KeyDetector(char shellDesign, ref Spaceship playerShip, ref List<Shell> shells)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo pressedKey = Console.ReadKey(true);

                // clear pressed key buffer to reduce lag
                while (Console.KeyAvailable) Console.ReadKey();

                // spaceship movement
                if (pressedKey.Key == ConsoleKey.LeftArrow)
                {
                    if (playerShip.x - 2 >= 0)
                    {
                        playerShip.x--;
                    }
                }
                else if (pressedKey.Key == ConsoleKey.RightArrow)
                {
                    // spaceship going to the end of the screen?
                    if (playerShip.x + playerShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length < Console.WindowWidth)
                    {
                        playerShip.x++;
                    }
                }


                // handle player firing
                if (pressedKey.Key == ConsoleKey.Spacebar || pressedKey.Modifiers == ConsoleModifiers.Control)
                {
                    shells = Shoot(shells, playerShip.x + (playerShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length) / 2,
                                    playerShip.y, shellDesign, ConsoleColor.Red);
                    // ToDo: laser sound
                }

                //Escape to main
                if (pressedKey.Key == ConsoleKey.Escape)
                {
                    MainPage();
                }
            }
        }

        // Move enemy ships
        private static /*List<Spaceship>*/ void MoveEnemies(ref List<Spaceship> enemyShips, ref bool moveLeft)
        {
            List<Spaceship> movedEnemies = new List<Spaceship>(enemyShips.Count);
            bool moveDown = false;

            // is the most left ship at x = 0
            if (MostLeftShip(enemyShips) == 0)
            {
                // if so, mvoe it to the right
                moveLeft = false;
                moveDown = true;
            }
            // if the most right ship is at the right end of the console
            else if (MostRightShip(enemyShips) == Console.BufferWidth - 1)
            {
                moveLeft = true;
                moveDown = true;
            }

            // move the ships
            Spaceship currentShip = new Spaceship();
            for (int i = 0; i < enemyShips.Count; i++)
            {
                currentShip.spaceshipDesign = enemyShips[i].spaceshipDesign;
                currentShip.color = enemyShips[i].color;
                currentShip.x = enemyShips[i].x;
                currentShip.y = enemyShips[i].y;

                // y coordinate
                if (moveDown)
                {
                    currentShip.y++;
                }

                // x coordinate
                if (moveLeft)
                {
                    currentShip.x--;
                }
                else
                {
                    currentShip.x++;
                }

                movedEnemies.Add(currentShip);
            }

            // save the new list into the old one
            enemyShips = movedEnemies;
        }

        /*
        private static void MoveEnemies(ref List<Spaceship> enemyShips, ref string enemyDirection)
        {
            List<Spaceship> newList = new List<Spaceship>();
            if (enemyDirection == "right")
            {
                Spaceship oldShipMostRight = enemyShips[enemyShips.Count - 1];
                for (int i = 0; i < enemyShips.Count; i++)
                {
                    Spaceship oldShip = enemyShips[i];

                    if (oldShipMostRight.x + oldShipMostRight.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length < Console.WindowWidth)
                    {
                        Spaceship newShip = new Spaceship();
                        newShip.x = oldShip.x + 1;
                        newShip.spaceshipDesign = oldShip.spaceshipDesign;
                        newShip.color = oldShip.color;
                        newShip.lives = oldShip.lives;
                        newShip.y = oldShip.y;
                        newList.Add(newShip);
                    }
                    else
                    {
                        newList.Add(oldShip);
                    }
                }

                if (oldShipMostRight.x + oldShipMostRight.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length == Console.WindowWidth)
                {
                    enemyDirection = "downFromRight";
                }
            }
            else if (enemyDirection == "downFromRight")
            {
                Spaceship oldShipMostFirst = enemyShips[0];
                for (int i = 0; i < enemyShips.Count; i++)
                {
                    Spaceship oldShip = enemyShips[i];
                    Spaceship newShip = new Spaceship();
                    newShip.x = oldShip.x;
                    newShip.spaceshipDesign = oldShip.spaceshipDesign;
                    newShip.color = oldShip.color;
                    newShip.lives = oldShip.lives;
                    newShip.y = oldShip.y + 1;
                    newList.Add(newShip);
                }

                enemyDirection = "left";
            }
            else if (enemyDirection == "left")
            {
                Spaceship oldShipMostFirst = enemyShips[0];
                for (int i = 0; i < enemyShips.Count; i++)
                {
                    Spaceship oldShip = enemyShips[i];
                    if (oldShipMostFirst.x + oldShipMostFirst.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Length > 0)
                    {
                        Spaceship newShip = new Spaceship();
                        newShip.x = oldShip.x - 1;
                        newShip.spaceshipDesign = oldShip.spaceshipDesign;
                        newShip.color = oldShip.color;
                        newShip.lives = oldShip.lives;
                        newShip.y = oldShip.y;
                        newList.Add(newShip);
                    }
                    else
                    {
                        newList.Add(oldShip);
                    }
                }
                if (oldShipMostFirst.x == 1)
                {
                    enemyDirection = "downFromLeft";
                }
            }
            else if (enemyDirection == "downFromLeft")
            {
                Spaceship oldShipMostFirst = enemyShips[0];
                for (int i = 0; i < enemyShips.Count; i++)
                {
                    Spaceship oldShip = enemyShips[i];
                    Spaceship newShip = new Spaceship();
                    newShip.x = oldShip.x;
                    newShip.spaceshipDesign = oldShip.spaceshipDesign;
                    newShip.color = oldShip.color;
                    newShip.lives = oldShip.lives;
                    newShip.y = oldShip.y + 1;
                    newList.Add(newShip);
                }

                enemyDirection = "right";
            }
            enemyShips = newList;
        }
        */

        // Draw a spaceship on the screen.
        static void DrawSpaceship(Spaceship ship)
        {
            // draw the spaceshipwrite every new line of the
            // spaceship in a different entry in a string array
            string[] spaceshipLines = ship.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            // draw the spaceship itself, line by line
            for (int i = 0; i < spaceshipLines.Length; i++)
            {
                // draw the ship
                buffer.AddToBuffer(spaceshipLines[i], (short)ship.x, (short)(ship.y + i), ship.color);
            }
        }

        // Draw the scoreboard
        static void DrawScoreboard()
        {
            // add the scoreboard to the buffer
            buffer.AddToBuffer(new String('-', Console.WindowWidth), 0, (short)(Console.WindowHeight - 2));
            buffer.AddToBuffer("Lives: " + playerLives, 0, (short)(Console.WindowHeight - 1));
            buffer.AddToBuffer("Score: " + playerScore, (short)(Console.WindowWidth / 2), (short)(Console.WindowHeight - 1));
        }

        private static List<Shell> Shoot(List<Shell> shells, int shipX, int shipY, char shellDesign, ConsoleColor color, bool goingUp = true)
        {
            Shell currentShell = new Shell();

            currentShell.x = shipX;
            currentShell.y = shipY;
            currentShell.shellDesign = shellDesign;
            currentShell.color = color;
            currentShell.goingUp = goingUp;

            //when we shoot we take the coordinates of the ship and add the shots into the list with all the shots
            shells.Add(currentShell);

            return shells;
        }

        private static List<enemyShell> EnemyShoot(List<enemyShell> enemyShells, int shipX, int shipY, char enemyShellDesign, ConsoleColor color, bool goingDown = true)
        {
            enemyShell currentenemyShell = new enemyShell();

            currentenemyShell.x = shipX;
            currentenemyShell.y = shipY;
            currentenemyShell.enemyShellDesign = enemyShellDesign;
            currentenemyShell.color = color;
            currentenemyShell.goingDown = goingDown;
            enemyShells.Add(currentenemyShell);

            return enemyShells;
        }

        // draw the shells
        static void DrawShells(List<Shell> shells)
        {
            // draw all the shells
            foreach (Shell currentShell in shells)
            {
                // add the shell to the buffer
                buffer.AddToBuffer(currentShell.shellDesign.ToString(), (short)currentShell.x, (short)currentShell.y, currentShell.color);
            }
        }

        // draw the shells
        static void DrawEnemyShells(List<enemyShell> enemyShells)
        {
            // draw all the shells
            foreach (enemyShell currentenemyShell in enemyShells)
            {
                // add the shell to the buffer
                buffer.AddToBuffer(currentenemyShell.enemyShellDesign.ToString(), (short)currentenemyShell.x, (short)currentenemyShell.y, currentenemyShell.color);
            }
        }

        // Method that prints text files. Parameters - string with path and file name and time (ms). Files are located in main project directory(where the .cs file is located)
        static void PrintTxtFile(string path, int time, int sleep)
        {
            StreamReader file = new StreamReader(path);

            using (file)
            {
                string line = file.ReadLine();

                while (line != null)
                {
                    if (Console.KeyAvailable)
                    {
                        time = 0;
                        break;
                    }

                    Console.WriteLine(line);
                    line = file.ReadLine();
                    Thread.Sleep(sleep);
                }
            }

            for (int i = 50; i <= time; i += 50)
            {
                Thread.Sleep(i);

                if (Console.KeyAvailable)
                {
                    break;
                }
            }
        }

        // Move the shells upward/downward
        static List<Shell> UpdateShells(List<Shell> shells)
        {
            // init the needed variables
            List<Shell> movedShells = new List<Shell>(shells.Count);
            Shell currentShell = new Shell();

            // move each shell upwards/downwards
            for (int i = 0; i < shells.Count; i++)
            {
                // if the shell has reached the end of the screen, skip this loop
                if (shells[i].y == 0 || shells[i].y == Console.BufferHeight)
                {
                    continue;
                }

                // add the appropriate values
                currentShell.shellDesign = shells[i].shellDesign;
                currentShell.color = shells[i].color;
                currentShell.x = shells[i].x;
                currentShell.goingUp = shells[i].goingUp;

                // move the shell up/down
                if (currentShell.goingUp)
                {
                    // move the shell up
                    currentShell.y = shells[i].y - 1;
                }
                else
                {
                    // move the shell down
                    currentShell.y = shells[i].y + 1;
                }

                // add the current shell to the new list of moved shells
                movedShells.Add(currentShell);
            }

            // return the new list
            return movedShells;
        }

        // Move the enemyshells udownward
        static List<enemyShell> UpdateEnemyShells(List<enemyShell> enemyShells)
        {
            // init the needed variables
            List<enemyShell> movedShells = new List<enemyShell>(enemyShells.Count);
            enemyShell currentShell = new enemyShell();

            // move each shell upwards/downwards
            for (int i = 0; i < enemyShells.Count; i++)
            {
                // if the shell has reached the end of the screen, skip this loop
                if (enemyShells[i].y == 0 || enemyShells[i].y == Console.BufferHeight)
                {
                    continue;
                }

                // add the appropriate values
                currentShell.enemyShellDesign = enemyShells[i].enemyShellDesign;
                currentShell.color = enemyShells[i].color;
                currentShell.x = enemyShells[i].x;
                currentShell.goingDown = enemyShells[i].goingDown;

                // move the shell down
                if (currentShell.goingDown)
                {
                    currentShell.y = enemyShells[i].y + 1;
                    currentShell.x = enemyShells[i].x + 1;
                }
                else
                {
                    currentShell.y = enemyShells[i].y - 1;
                }
             
                // add the current shell to the new list of moved shells
                movedShells.Add(currentShell);
            }

            // return the new list
            return movedShells;
        }

        // Second home page screen
        static void MainPage()
        {
            Console.ResetColor();
            Console.WriteLine(@"




               _________________________________________________
              |                                                 |
              |                      __|__                      |
              |                       _|_                       |
              |                      / _ \                      |
              |                   __/ (_) \__                   |
              |              ____/_ ======= _\____              |
              |     ________/ _ \(_)_______(_)/ _ \________     |
              |    <___+____ (_) | /   _   \ | (_) ____+___>    |
              |      O O O  \___/ |   (_)   | \___/  O O O      |
              |                 \__\_______/__/                 |
              |                                                 |
              |    ________   __   ___   _  _________   _  __   |
              |   / ___/ _ | / /  / _ | | |/_/  _/ _ | / |/ /   |
              |  / (_ / __ |/ /__/ __ |_>  <_/ // __ |/    /    |
              |  \___/_/ |_/____/_/ |_/_/|_/___/_/ |_/_/|_/     |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              |             Press [Enter] key to start          |
              |                                                 |
              |                                                 |
              |                                                 |
              |                                                 |
              | Team Fujin                    Telerik Academy   |
              |_________________________________________________|






                                  Game controls:
                    
                      Use Left arrow ( <- ) to move left.
                     Use Right arrow ( -> ) to move rigth.
                       Use Spacebar ot Ctrl key to shoot.
                     Use Ecs key to return to Main screen.

                           Press ""Q"" to exit game.






");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.Q)
            {
                Console.Clear();
                Console.WriteLine("Bye, bye!!");
                Environment.Exit(0);
            }
            else if (keyInfo.Key != ConsoleKey.Enter)
            {
                MainPage();
                Console.WriteLine("");
            }
        }

        // STAR!S
        // Generate stars
        static List<Star> GenerateStars(int count)
        {
            // init variables
            List<Star> stars = new List<Star>(count);
            Random randNumber = new Random();
            Star currentStar = new Star();

            // iterate count times through the empty list
            // adding the stars
            for (int i = 0; i < count; i++)
            {
                // generate random star coordinates
                currentStar.x = randNumber.Next(0, Console.WindowWidth);
                currentStar.y = randNumber.Next(0, Console.WindowHeight);

                // the star is a dot (.)
                currentStar.starDesign = '.';

                // add the star we generated in this iteration
                // to the list of stars
                stars.Add(currentStar);
            }

            // return the list with stars
            return stars;
        }

        // Draw the stars
        static void DrawStars(List<Star> stars)
        {
            // iterate through all the stars
            for (int i = 0; i < stars.Count; i++)
            {
                // draw the stars at the appropriate coordinates
                //Console.SetCursorPosition(stars[i].x, stars[i].y);
                //Console.Write(stars[i].starDesign);
                buffer.AddToBuffer(stars[i].starDesign.ToString(), (short)stars[i].x, (short)stars[i].y);
            }
        }

        // Move the stars
        static List<Star> MoveStars(List<Star> oldStars)
        {
            // init variables
            List<Star> newStars = new List<Star>(50);
            Star currentStar = new Star();
            Random randNumber = new Random();

            // move every star
            for (int i = 0; i < oldStars.Count; i++)
            {
                // check whether the current star has reached
                // the end of the screen
                if (oldStars[i].y + 1 < Console.WindowHeight)
                {
                    // if it hasn't reached the end, move one position down
                    currentStar.x = oldStars[i].x;
                    currentStar.y = oldStars[i].y + 1;
                }
                else
                {
                    // if it has, generate a new star at the top of the screen
                    currentStar.y = 0;
                    currentStar.x = randNumber.Next(0, Console.WindowWidth);
                }

                // star design
                currentStar.starDesign = oldStars[i].starDesign;

                // add the current star to the list
                newStars.Add(currentStar);
            }

            // return the new list with moved stars
            return newStars;
        }
        // end of STARS !
        
        // pravi prowerka dali ima sblusuk meghdu vragheskata strelba i koraba na igracha
        private static bool isEnemyShipHitPlayer(Spaceship playerShip, List<List<int>> shots)
        {
            bool isPlayerHited = false;
            string[] playerSpaceshipLines = playerShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            int playerHight = playerSpaceshipLines.GetLength(0);
            int playerWidth = playerSpaceshipLines[1].Length;
            //prowerqwa za wseki edin shot ot lista dali ne e uluchil koraba na igracha
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i][1] >= playerShip.y && shots[i][1] <= playerShip.y + playerHight - 1
                    && shots[i][0] >= playerShip.x && shots[i][0] <= playerShip.x + playerWidth - 1)
                {
                    //proverqwa w mqstoto na sblusuka meghdu shota i koraba dali ima simvoli w matricata na koraba
                    if (playerSpaceshipLines[shots[i][1] - playerShip.y][shots[i][0] - playerShip.x] != ' ')
                    {
                        isPlayerHited = true;
                        //playerShip.lives--; // towa moghe i na drugo mqsto da se sloghi
                        return isPlayerHited;
                    }
                }
            }
            return isPlayerHited;
        }

        // pravi prowerka dali ima sblusuk meghdu vragheskiq korab i tozi na igracha
        private static bool isEnemyShipBumpPlayer(Spaceship playerShip, Spaceship enemyShip)
        {
            string[] playerSpaceshipLines = playerShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            string[] enemySpaceshipLines = enemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            bool isHit = false;
            int enemyHight = enemySpaceshipLines.GetLength(0);
            int enemyWidth = enemySpaceshipLines[0].Length;

            int playerHight = playerSpaceshipLines.GetLength(0);
            int playerWidth = playerSpaceshipLines[1].Length;

            int maxAreaHight = 0;
            int maxAreaWidth = 0;
            //proverka dali dwata koraba se zastupwav po x i y, kato tova e sluchaq v koito vragheskiq korab e ot lqvo na koranba na igracha
            if ((enemyShip.y + enemyHight - 1 >= playerShip.y)
                && (enemyShip.x + enemyWidth - 1) >= playerShip.x && (enemyShip.x + enemyWidth - 1) <= (playerShip.x + playerWidth - 1))
            {
                maxAreaHight = enemyShip.y + enemyHight - playerShip.y;
                maxAreaWidth = enemyShip.x + enemyWidth - playerShip.x;
                //ako ima zastupvane prowerqwa w uchastuka na sblusuka za wsqka koordinata dali ima simvoli suotwetno vuv vragheskiq korab na tezi koordinati i na kodaba na igracha 
                for (int i = 0; i < maxAreaHight; i++)
                {
                    for (int j = 0; j < maxAreaWidth; j++)
                    {
                        if (enemyHight - 1 - i >= 0 && enemyWidth - 1 - j >= 0 && maxAreaHight - 1 - i >= 0 && maxAreaWidth - 1 - j >= 0)
                        {
                            if (enemySpaceshipLines[enemyHight - 1 - i][enemyWidth - 1 - j] != ' '
                               && playerSpaceshipLines[maxAreaHight - 1 - i][maxAreaWidth - 1 - j] != ' ')
                            {
                                isHit = true;
                                //playerShip.lives--; // towa moghe i na drugo mqsto da se sloghi
                                return isHit;
                            }
                        }
                    }
                }
            }
            //proverka dali dwata koraba se zastupwav po x i y, kato tova e sluchaq v koito vragheskiq korab e ot dqsno na koraba na igracha
            else if ((enemyShip.y + enemyHight - 1 >= playerShip.y)
                && (enemyShip.x >= playerShip.x) && (enemyShip.x <= (playerShip.x + playerWidth - 1)))
            {
                maxAreaHight = enemyShip.y + enemyHight - playerShip.y;
                maxAreaWidth = playerShip.x + playerWidth - enemyShip.x;
                //ako ima zastupvane prowerqwa w uchastuka na sblusuka za wsqka koordinata dali ima simvoli suotwetno vuv vragheskiq korab na tezi koordinati i na kodaba na igracha
                for (int i = 0; i < maxAreaHight; i++)
                {
                    for (int j = 0; j < maxAreaWidth; j++)
                    {
                        if (enemyHight - 1 - i >= 0 && maxAreaHight - 1 - i >= 0 && playerWidth - maxAreaWidth + j >= 0)
                        {
                            if (enemySpaceshipLines[enemyHight - 1 - i][j] != ' '
                               && playerSpaceshipLines[maxAreaHight - 1 - i][playerWidth - maxAreaWidth + j] != ' ') // -1? player j
                            {
                                isHit = true;
                                //playerShip.lives--; // towa moghe i na drugo mqsto da se sloghi
                                return isHit;
                            }
                        }
                    }
                }
            }
            return isHit;
        }

        // pravi prowerka na vseki edin "wragheski" korab dali ne e udaril tozi na igracha 
        private static bool CheckEveryEnemyShipBumpPlayer(List<Spaceship> enemyShips, Spaceship playerShip)
        {
            bool isEnShipBumpPlayes = false;
            foreach (var enemyShip in enemyShips)
            {
                isEnShipBumpPlayes = isEnemyShipBumpPlayer(playerShip, enemyShip);
                if (isEnShipBumpPlayes)
                {
                    return isEnShipBumpPlayes;
                }                
            }
            return isEnShipBumpPlayes;
        }

        private static bool isPlayerShipHitEnemy( List<Shell> shells,List<Spaceship> enemyShips)
        {
            bool IsEnemyHit = false;
            for (int j = 0; j < enemyShips.Count; j++)
            {
                Shell currentShell = new Shell();
                Spaceship enemyShip = enemyShips[j];
                string[] EnemySpaceshipLines = enemyShip.spaceshipDesign.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                int playerHight = EnemySpaceshipLines.GetLength(0);
                int playerWidth = EnemySpaceshipLines[1].Length;

                for (int i = 0; i < shells.Count; i++)
                {
                    currentShell = shells[i];
                    if (currentShell.y >= enemyShip.y && currentShell.y <= enemyShip.y + playerHight - 1
                        && currentShell.x >= enemyShip.x && currentShell.x <= enemyShip.x + playerWidth - 1)
                    {

                        if (EnemySpaceshipLines[currentShell.y - enemyShip.y][currentShell.x - enemyShip.x] != ' ')
                        {
                            IsEnemyHit = true;
                            enemyShips.Remove(enemyShip);// премахваме вражеският кораб от списъка с кораби
                            foreach (var line in EnemySpaceshipLines)
                            {
                                enemyShip.spaceshipDesign = "  ";
                            }

                            UpdateScreen(); //  би трябвало екрана да се обнови след като се "изтрие" ударения кораб
                            return IsEnemyHit;
                        }
                    }
                }
            }
            return IsEnemyHit;
         }
     
        // ToDO method for uPDting the screen
        static void UpdateScreen()
        { 
        
        
        
        }
      

    } // class

} // namespace