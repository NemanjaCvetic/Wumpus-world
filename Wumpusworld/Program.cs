using System.Diagnostics;
using System.Text;

internal class Program
{


    /*

    In this assignment you'll need to implement a variant of the »Wumpus world« game (see: AIMA3e book, chapter 7.2, pp. 236 - 240). The goal of the game is to »safely« traverse the cave and exit with a maximal number of points. The definition of the »Wumpus world« game is the same as that from the book with the following corrections:

    The gold is not just one, but there can be many of it scattered on different places of the cave;
    The size of the cave is M x N, where M, N, the position of pits, gold and the Wumpus are given in advance in the form of a text file; 
    (wumpus_world.txt - the exact format is given later on);
    The price of shooting the (only) arrow is -100 points;
    The start position of the agent is facing right.

Your assignment is to:

    Implement a »smart« agent capable of navigating the cave, avoiding pits and the Wumpus, picking up as much gold as possible (or maximise the number of points), and reach the exit point of the cave - field (x,y) that is defined in advance as the goal field. Agent's input should be the text file wumpus_world.txt. The output should be the »trace« of the agent together with all logical entailments for every move of the agent;
    Generate test worlds (at least three) - files »wumpus_world.txt« - that will serve as the input for the agent;
    Write a report describing your solution. The report should include: the description of the methods that you tested (can be in pseudocode), the description of the first order logic part of the agent's reasoning, the description of problems that you encountered during the process of implementing your solutions.

You'll implement the agent as a hybrid algorithm that integrates logical induction, search and background knowledge (knowledge base). You have to take into consideration the following constraints:

    Your agent has to integrate with some knowledge base in the form of a TELL-ASK interface: with the TELL command the perceptions of the agent are entered into the knowledge base, the ASK command is used to query the knowledge base if a determined field is »safe«. The knowledge base should be a first order logic base. 

    You need to implement a search algorithm (A* or some other search algorithm) to plan a route to a given field and interface it to the agent;
    Your agent can read the whole cave map at once, but must not »see« fields, that it didn't yet visit - e.g. it should not know that there's gold on the field (2,3) until it visits that field. The only exceptions are the labels Mxy, Axy and GOxy - see below for »label meaning«. To be sure your agents doesn't »peep«, you'll have to output every move that the agent makes, e.g.:

        Move to field (2,1)
        Breeze sensed
        Move to field (1,1)
        Move to field (1,2)
        Smell sensed
        Found out Wumpus is on field (1,3)
        Found out pit is on field (3,1)
        Move to field (2,2)

Representation of the Wumpus world:

    Example of the wumpus_world.txt file:

A11

B21

P31

B41

S12

B32

W13

S23

G23

B23

P33

B43

S14

B34

P44

M44

GO11

    Meaning of labels in the above example:

Axy  = Agent is on the field (x,y)

Bxy  = The field (x,y) is breezy

Gxy  = There's gold on the field (x,y)

GOxy = (x,y) is the goal field - exit from the cave

Mxy  = The cave is x fields wide and y fields high (map size)

Pxy  = There's a pit on the field (x,y)

Sxy  = The field (x,y) is smelly

Wxy  = There's the Wumpus on the field (x,y)

    */



    private static void Main(string[] args)
    {

        Console.WriteLine("Welcome to the Wumpus World!");
        KnowledgeBase kb = new KnowledgeBase();

        while (true)
        {
            Console.Write("Enter the filename for the Wumpus World (or 'quit' to exit): ");
            string filename = Console.ReadLine();

            if (filename.ToLower() == "quit")
                break;

            try
            {
                WumpusWorld world = new WumpusWorld(filename);
                 bool gameCompleted = false;
                while (!gameCompleted)
                {
                    Agent agent = new Agent(world, kb);
                    Console.WriteLine($"Starting a new attempt in the world from {filename}...");
                    agent.Play();

                    if (agent.ReachedGoal)
                    {
                        gameCompleted = true;
                        Console.WriteLine("Game completed successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Agent died. Restarting from the beginning...");
                        world.ResetAgentPosition();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        Console.WriteLine("Thank you for playing Wumpus World!");

    }

    enum Direction { Right, Down, Left, Up }

    class WumpusWorld
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private Dictionary<(int, int), List<string>> world;
        public (int, int) AgentPosition { get; set; }
        public (int, int) GoalPosition { get; private set; }

         public (int, int) InitialAgentPosition { get; private set; }

        public WumpusWorld(string filename)
        {
            world = new Dictionary<(int, int), List<string>>();
            ParseWorldFile(filename);
             InitialAgentPosition = AgentPosition;
        }

        private void ParseWorldFile(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Length < 3)
                {
                    Console.WriteLine($"Warning: Skipping invalid line: {line}");
                    continue;
                }

                char label = line[0];
                int x, y;

                if (label == 'G' && line.Length > 3 && line[1] == 'O')
                {
                    if (!int.TryParse(line.Substring(2, 1), out x) || !int.TryParse(line.Substring(3, 1), out y))
                    {
                        Console.WriteLine($"Warning: Invalid goal format: {line}");
                        continue;
                    }
                    GoalPosition = (x, y);
                    continue;
                }

                if (!int.TryParse(line.Substring(1, 1), out x) || !int.TryParse(line.Substring(2, 1), out y))
                {
                    Console.WriteLine($"Warning: Invalid coordinate format: {line}");
                    continue;
                }

                if (!world.ContainsKey((x, y)))
                    world[(x, y)] = new List<string>();

                switch (label)
                {
                    case 'A':
                        AgentPosition = (x, y);
                        InitialAgentPosition = (x, y);
                        break;
                    case 'M':
                        Width = x;
                        Height = y;
                        break;
                    case 'G':
                        world[(x, y)].Add("Gold");
                        break;
                    case 'P':
                        world[(x, y)].Add("Pit");
                        break;
                    case 'W':
                        world[(x, y)].Add("Wumpus");
                        break;
                    case 'B':
                        world[(x, y)].Add("Breeze");
                        break;
                    case 'S':
                        world[(x, y)].Add("Smell");
                        break;
                    default:
                        Console.WriteLine($"Warning: Unknown label '{label}' in line: {line}");
                        break;
                }
            }
        }

       public void ResetAgentPosition()
    {
        AgentPosition = InitialAgentPosition;
    }

        public List<string> GetCell(int x, int y)
        {
            if (world.TryGetValue((x, y), out var cellContents))
            {
                return new List<string>(cellContents);  // Return a copy of the list
            }
            return new List<string>();
        }
    }

    class Agent
    {
        private WumpusWorld world;
        private KnowledgeBase kb;
        private List<(int, int)> visitedCells;
        private Direction facing;
        private int score;
        private bool hasArrow;
        private List<string> actionLog;
        private bool isAlive;
        private Queue<(int, int)> plannedPath;


        public bool ReachedGoal { get; private set; }

        public Agent(WumpusWorld world, KnowledgeBase kb)
        {
            this.world = world;
            this.kb = kb;
            this.visitedCells = new List<(int, int)>();
            this.facing = Direction.Right;
            this.score = 0;
            this.hasArrow = true;
            this.actionLog = new List<string>();
            this.isAlive = true;
            this.plannedPath = new Queue<(int, int)>();
            this.ReachedGoal = false;
        }

        public void Play()
        {
            while (isAlive)
            {
                (int, int) nextMove = PlanNextMove();
                Move(nextMove);

                 if (!isAlive)
            {
                LearnFromDeath();
                break;
            }

                if (world.AgentPosition == world.GoalPosition)
                {
                    ReachedGoal = true;
                    Log($"Reached the goal! Final score: {score}");
                    break;
                }
            }

            PrintActionLog();
        }

        private void LearnFromDeath()
    {
        kb.Tell($"Pit({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
        Log($"Learned that there is a pit at {world.AgentPosition}");
    }

        private void PerceiveEnvironment()
        {
            var perceptions = world.GetCell(world.AgentPosition.Item1, world.AgentPosition.Item2);
            LogDebug($"Perceiving environment at {world.AgentPosition}");
            bool breezeDetected = false;
            bool stenchDetected = false;

            foreach (var perception in perceptions)
            {
                LogDebug($"  Perceived: {perception}");
                HandlePerception(perception);
                if (perception == "Breeze") breezeDetected = true;
                if (perception == "Smell") stenchDetected = true;
            }

            UpdateAdjacentCellsKnowledge(breezeDetected, stenchDetected);
            DeducePitLocations();
            DeduceWumpusLocation();
        }

        private void HandlePerception(string perception)
        {
            switch (perception)
            {
                case "Breeze":
                    kb.Tell($"Breeze({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
                    Log($"Breeze sensed at {world.AgentPosition}", false);
                    break;
                case "Smell":
                    kb.Tell($"Stench({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
                    Log($"Smell sensed at {world.AgentPosition}", false);
                    break;
                case "Gold":
                    kb.Tell($"Gold({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
                    Log($"Gold found at {world.AgentPosition}", false);
                    score += 1000;
                    break;
                case "Pit":
                    kb.Tell($"Pit({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
                    Log($"Found out pit is on field {world.AgentPosition}", false);
                    isAlive = false;
                    score -= 1000;
                    Log($"Agent died! Stepped into a Pit");
                    break;
                case "Wumpus":
                    if (!kb.Ask($"DeadWumpus({world.AgentPosition.Item1},{world.AgentPosition.Item2})"))
                    {
                        kb.Tell($"Wumpus({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
                        Log($"Found out Wumpus is on field {world.AgentPosition}", false);
                        isAlive = false;
                        score -= 1000;
                        Log($"Agent died! Stepped into a live Wumpus");
                    }
                    else
                    {
                        Log($"Encountered a dead Wumpus at {world.AgentPosition}", false);
                    }
                    break;
            }
        }

        private void DeduceWumpusLocation()
        {
            if (kb.Ask($"Stench({world.AgentPosition.Item1},{world.AgentPosition.Item2})"))
            {
                var adjacentCells = GetAdjacentCells(world.AgentPosition);
                var possibleWumpusCells = adjacentCells.Where(cell =>
                    !kb.Ask($"Visited({cell.Item1},{cell.Item2})") &&
                    !kb.Ask($"NoWumpus({cell.Item1},{cell.Item2})")
                ).ToList();

                if (possibleWumpusCells.Count == 1)
                {
                    var wumpusCell = possibleWumpusCells[0];
                    if (!kb.Ask($"Wumpus({wumpusCell.Item1},{wumpusCell.Item2})"))
                    {
                        kb.Tell($"Wumpus({wumpusCell.Item1},{wumpusCell.Item2})");
                        Log($"Deduced that the Wumpus is on field ({wumpusCell.Item1},{wumpusCell.Item2})", false);
                    }
                }
                else if (IsOnEdge(world.AgentPosition))
                {
                    var wumpusCell = GetWumpusPositionFromStench(world.AgentPosition);
                    kb.Tell($"Wumpus({wumpusCell.Item1},{wumpusCell.Item2})");
                    Log($"Deduced that the Wumpus is on field ({wumpusCell.Item1},{wumpusCell.Item2})");
                }
            }
        }

        private bool IsOnEdge((int, int) position)
        {
            return position.Item1 == 1 || position.Item1 == world.Width ||
                   position.Item2 == 1 || position.Item2 == world.Height;
        }


        private void DeducePitLocations()
        {
            for (int x = 1; x <= world.Width; x++)
            {
                for (int y = 1; y <= world.Height; y++)
                {
                    if (kb.IsSafe(x, y))
                    {
                        continue; // Skip known safe squares
                    }

                    LogDebug($"Checking pit deduction for ({x},{y}):");
                    LogDebug($"  PossiblePit: {kb.Ask($"PossiblePit({x},{y}")}");
                    //LogDebug($"  Visited: {!kb.Ask($"Visited({x},{y}")}");
                    // LogDebug($"  NoPit: {kb.Ask($"NoPit({x},{y}")}");
                    if (!kb.IsVisited(x, y) && !kb.Ask($"NoPit({x},{y})"))  //&& !kb.Ask($"Visited({x},{y})")
                    {
                        LogDebug($"  Deducing pit for ({x},{y})");
                        var adjacentCells = GetAdjacentCells((x, y));
                        var breezyAdjacentCells = adjacentCells.Where(cell =>
                            kb.Ask($"Visited({cell.Item1},{cell.Item2})") &&
                            kb.Ask($"Breeze({cell.Item1},{cell.Item2})")
                        ).ToList();

                        var nonBreezyAdjacentCells = adjacentCells.Where(cell =>
                           kb.IsVisited(cell.Item1, cell.Item2) &&
                            !kb.Ask($"Breeze({cell.Item1},{cell.Item2})")
                        ).ToList();

                        if (breezyAdjacentCells.Count > 0 && nonBreezyAdjacentCells.Count > 0)
                        {
                            kb.Tell($"NoPit({x},{y})");
                            kb.MarkSafe(x, y);
                        }
                        else if (breezyAdjacentCells.Count > 0)
                        {
                            kb.Tell($"PossiblePit({x},{y})");
                        }

                        /*         else if (IsOnEdge(world.AgentPosition))
                          {
                              var pitCell = GetPitPositionFromBreeze(world.AgentPosition);
                              kb.Tell($"Pit({pitCell.Item1},{pitCell.Item2})");
                              Log($"Deduced that the Pit is on field ({pitCell.Item1},{pitCell.Item2})");
                          }

                           */
                    }
                    else
                    {
                        LogDebug($"  Skipping pit deduction for ({x},{y})");
                    }
                }
            }
        }

        private (int, int) GetPitPositionFromBreeze((int, int) agentPosition)
        {
            if (agentPosition.Item1 == 1)
                return (1, agentPosition.Item2 + 1);
            else if (agentPosition.Item1 == world.Width)
                return (world.Width - 1, agentPosition.Item2);
            else if (agentPosition.Item2 == 1)
                return (agentPosition.Item1, 2);
            else // agentPosition.Item2 == world.Height
                return (agentPosition.Item1, world.Height - 1);
        }

        private void LogDebug(string message)
        {
            Console.WriteLine($"[DEBUG] Turn {turnCount}: {message}");
        }








        private (int, int) GetWumpusPositionFromStench((int, int) agentPosition)
        {
            if (agentPosition.Item1 == 1)
                return (1, agentPosition.Item2 + 1);
            else if (agentPosition.Item1 == world.Width)
                return (world.Width, agentPosition.Item2 + 1);
            else if (agentPosition.Item2 == 1)
                return (agentPosition.Item1, 2);
            else // agentPosition.Item2 == world.Height
                return (agentPosition.Item1, world.Height - 1);
        }




        private void UpdateAdjacentCellsKnowledge(bool breezeDetected, bool stenchDetected)
        {
            var adjacentCells = GetAdjacentCells(world.AgentPosition);

            foreach (var cell in adjacentCells)
            {
                if (!kb.Ask($"Visited({cell.Item1},{cell.Item2})"))
                {
                    if (!breezeDetected)
                    {
                        kb.Tell($"NoPit({cell.Item1},{cell.Item2})");
                        kb.MarkSafe(cell.Item1, cell.Item2);
                    }
                    else
                    {
                        kb.Tell($"PossiblePit({cell.Item1},{cell.Item2})");
                    }

                    if (!stenchDetected)
                    {
                        kb.Tell($"NoWumpus({cell.Item1},{cell.Item2})");
                        kb.MarkSafe(cell.Item1, cell.Item2);
                    }
                    else
                    {
                        kb.Tell($"PossibleWumpus({cell.Item1},{cell.Item2})");
                    }
                }
            }
        }

        private (int, int) PlanNextMove()
        {
            LogDebug("Planning next move");
            if (plannedPath.Count == 0)
            {
                LogDebug("No planned path, generating new path");
                plannedPath = PlanPath();
            }

            var adjacentCells = GetAdjacentCells(world.AgentPosition);
             var safeCells = adjacentCells.Where(cell => 
            kb.IsSafe(cell.Item1, cell.Item2) && 
            !kb.Ask($"Pit({cell.Item1},{cell.Item2})") &&
            !kb.Ask($"Wumpus({cell.Item1},{cell.Item2})")
        ).ToList();
            if (safeCells.Any())
            {
                return safeCells.OrderBy(DistanceToGoal).First();
            }

             var unexploredCells = adjacentCells.Where(cell => !kb.IsVisited(cell.Item1, cell.Item2)).ToList();
        if (unexploredCells.Any())
        {
            return unexploredCells.OrderBy(cell => DangerLevel(cell)).First();
        }

            if (plannedPath.Count > 0)
            {
                var nextMove = plannedPath.Peek();
                LogDebug($"Next planned move: {nextMove}");
                if (nextMove == world.AgentPosition)
                {
                    LogDebug("Next move is current position, removing from path");
                    plannedPath.Dequeue(); // Remove the current position from the path
                    return PlanNextMove(); // Recursively plan the next move
                }

                if (kb.Ask($"Wumpus({nextMove.Item1},{nextMove.Item2})") && !kb.Ask($"DeadWumpus({nextMove.Item1},{nextMove.Item2})"))
                {
                    if (hasArrow)
                    {
                        ShootArrow();
                        plannedPath.Clear(); // Clear the path after shooting to re-evaluate
                        return world.AgentPosition;  // Stay in place after shooting
                    }
                    else
                    {
                        // If we've shot and missed, we need to find a new path
                        plannedPath.Clear();
                        return GetSafestMove();
                    }
                }

                if (!kb.Ask($"Pit({nextMove.Item1},{nextMove.Item2})"))
                {
                    return plannedPath.Dequeue();
                }
            }
            LogDebug("No planned path, getting safest move");
            return adjacentCells.OrderBy(cell => DangerLevel(cell)).First();
        }


        private Queue<(int, int)> PlanPath()
        {
            var path = new Queue<(int, int)>();
            var target = ChooseTarget();

            if (target != world.AgentPosition)
            {
                path = AStar(world.AgentPosition, target);
            }

            return path;
        }

        private (int, int) ChooseTarget()
        {
            // Priority: Unvisited safe cells > Gold > Exit
            var unvisitedSafeCells = GetUnvisitedSafeCells();
            if (unvisitedSafeCells.Any())
            {
                return unvisitedSafeCells.OrderBy(DistanceToAgent).First();
            }

            var knownGoldCells = GetKnownGoldCells();
            if (knownGoldCells.Any())
            {
                return knownGoldCells.OrderBy(DistanceToAgent).First();
            }

            // Check if the Wumpus is known and avoid it
            if (kb.Ask($"Wumpus({world.AgentPosition.Item1},{world.AgentPosition.Item1})"))
            {
                var safeRouteToExit = AStar(world.AgentPosition, world.GoalPosition);
                if (safeRouteToExit.Count > 0)
                {
                    return safeRouteToExit.Dequeue();
                }
            }

            return world.GoalPosition;
        }

        private List<(int, int)> GetUnvisitedSafeCells()
        {
            var cells = new List<(int, int)>();
            for (int x = 1; x <= world.Width; x++)
            {
                for (int y = 1; y <= world.Height; y++)
                {
                    if (!kb.Ask($"Visited({x},{y})") && (kb.IsSafe(x, y) || IsSafe((x, y))))
                    {
                        cells.Add((x, y));
                    }
                }
            }
            return cells;
        }

        private List<(int, int)> GetKnownGoldCells()
        {
            var cells = new List<(int, int)>();
            for (int x = 1; x <= world.Width; x++)
            {
                for (int y = 1; y <= world.Height; y++)
                {
                    if (kb.Ask($"Gold({x},{y})") && !kb.Ask($"GoldCollected({x},{y})"))
                    {
                        cells.Add((x, y));
                    }
                }
            }
            return cells;
        }

        private Queue<(int, int)> AStar((int, int) start, (int, int) goal)
        {
            var frontier = new PriorityQueue<(int, int), int>();
            frontier.Enqueue(start, 0);

            var cameFrom = new Dictionary<(int, int), (int, int)?>();
            var costSoFar = new Dictionary<(int, int), int>();

            cameFrom[start] = null;
            costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                LogDebug($"AStar processing cell: {current}");


                if (current == goal)
                {
                    break;
                }

                foreach (var next in GetAdjacentCells(current).OrderByDescending(cell => kb.IsSafe(cell.Item1, cell.Item2)))
                {

                    if (kb.IsSafe(next.Item1, next.Item2) || IsSafe(next))
                    {

                        LogDebug($"  Considering adjacent cell: {next}");

                        int newCost = costSoFar[current] + 1;

                        if ((!costSoFar.ContainsKey(next) || newCost < costSoFar[next]) && IsSafe(next))
                        {
                            costSoFar[next] = newCost;
                            int priority = newCost + ManhattanDistance(next, goal);
                            frontier.Enqueue(next, priority);
                            cameFrom[next] = current;
                        }
                    }
                }
            }

            return ReconstructPath(cameFrom, start, goal);
        }

        private Queue<(int, int)> ReconstructPath(Dictionary<(int, int), (int, int)?> cameFrom, (int, int) start, (int, int) goal)
        {
            var path = new Queue<(int, int)>();
            (int, int)? current = goal;

            while (current != start)
            {
                if (current == null) return new Queue<(int, int)>();  // No path found
                path.Enqueue(current.Value);
                current = cameFrom[current.Value];
            }

            var reversedPath = new Queue<(int, int)>(path.Reverse());
            return reversedPath;
        }

        private int ManhattanDistance((int, int) a, (int, int) b)
        {
            return Math.Abs(a.Item1 - b.Item1) + Math.Abs(a.Item2 - b.Item2);
        }

        private (int, int) GetSafestMove()
        {
            var adjacentCells = GetAdjacentCells(world.AgentPosition);
            var safeCells = adjacentCells.Where(cell =>
                IsSafe(cell) && !kb.Ask($"Wumpus({cell.Item1},{cell.Item2})")
            ).ToList();

            if (safeCells.Any())
            {
                return safeCells.OrderBy(cell => kb.Ask($"PossiblePit({cell.Item1},{cell.Item2})") ? 1 : 0)
                                .ThenBy(DistanceToGoal)
                                .First();
            }

            // If no safe moves, consider shooting the arrow
            if (hasArrow)
            {
                ShootArrow();
                return world.AgentPosition;  // Stay in place after shooting
            }

            // If no safe moves and no arrow, take the least dangerous move
            return adjacentCells.OrderBy(cell => DangerLevel(cell)).First();
        }


        private int DangerLevel((int, int) cell)
        {
            int danger = 0;
        if (kb.Ask($"Pit({cell.Item1},{cell.Item2})")) danger += 1000;
        if (kb.Ask($"PossiblePit({cell.Item1},{cell.Item2})")) danger += 50;
        if (kb.Ask($"Wumpus({cell.Item1},{cell.Item2})")) danger += 1000;
        if (kb.Ask($"PossibleWumpus({cell.Item1},{cell.Item2})")) danger += 50;
        return danger;
        }

        private bool IsSafe((int, int) cell)
        {

            if (kb.IsSafe(cell.Item1, cell.Item2))
            {
                return true;
            }

            bool safe = !kb.Ask($"Pit({cell.Item1},{cell.Item2})") ||
                         kb.Ask($"NoPit({cell.Item1},{cell.Item2})") &&
                         !kb.Ask($"PossiblePit({cell.Item1},{cell.Item2})") &&
                        !kb.Ask($"Wumpus({cell.Item1},{cell.Item2})") &&
                         !kb.Ask($"PossibleWumpus({cell.Item1},{cell.Item2})");
            LogDebug($"IsSafe check for {cell}: {safe}");
            return safe;
        }

        private List<(int, int)> GetAdjacentCells((int, int) cell)
        {
            var adjacentCells = new List<(int, int)>
        {
            (cell.Item1 + 1, cell.Item2),
            (cell.Item1 - 1, cell.Item2),
            (cell.Item1, cell.Item2 + 1),
            (cell.Item1, cell.Item2 - 1)
        };

            return adjacentCells.Where(c => c.Item1 >= 1 && c.Item1 <= world.Width &&
                                            c.Item2 >= 1 && c.Item2 <= world.Height).ToList();
        }

        private void ShootArrow()
        {
            if (!hasArrow) return;

            hasArrow = false;
            score -= 100;
            Log("Arrow shot!");

            (int, int) target = GetTargetCell();
            if (world.GetCell(target.Item1, target.Item2).Contains("Wumpus"))
            {
                Log("Wumpus killed!");
                kb.Tell($"NoWumpus({target.Item1},{target.Item2})");
                kb.Tell($"DeadWumpus({target.Item1},{target.Item2})");
                // Remove stench from adjacent cells
                foreach (var cell in GetAdjacentCells(target))
                {
                    kb.Tell($"NoStench({cell.Item1},{cell.Item2})");
                }
            }
            else
            {
                Log("Arrow missed the Wumpus!");
            }
        }

        private (int, int) GetTargetCell()
        {
            switch (facing)
            {
                case Direction.Right: return (world.AgentPosition.Item1 + 1, world.AgentPosition.Item2);
                case Direction.Left: return (world.AgentPosition.Item1 - 1, world.AgentPosition.Item2);
                case Direction.Up: return (world.AgentPosition.Item1, world.AgentPosition.Item2 + 1);
                case Direction.Down: return (world.AgentPosition.Item1, world.AgentPosition.Item2 - 1);
                default: throw new InvalidOperationException("Invalid facing direction");
            }
        }

        private void Move((int, int) nextPosition)
        {
            LogDebug($"Considering move to {nextPosition}");
            LogDebug($"Current KB state for {nextPosition}:");
            LogDebug($"  IsSafe: {IsSafe(nextPosition)}");
            LogDebug($"  Pit: {kb.Ask($"Pit({nextPosition.Item1},{nextPosition.Item2}")}");
            LogDebug($"  PossiblePit: {kb.Ask($"PossiblePit({nextPosition.Item1},{nextPosition.Item2}")}");
            LogDebug($"  Wumpus: {kb.Ask($"Wumpus({nextPosition.Item1},{nextPosition.Item2}")}");
            LogDebug($"  PossibleWumpus: {kb.Ask($"PossibleWumpus({nextPosition.Item1},{nextPosition.Item2}")}");

            if (!IsSafe(nextPosition))
            {
                LogDebug($"Aborting move to {nextPosition} as it's not safe!");
                return;
            }

            UpdateFacing(nextPosition);
            world.AgentPosition = nextPosition;
            score -= 1; // Cost of moving
            kb.MarkVisited(nextPosition.Item1, nextPosition.Item2);
            Log($"Moved to {nextPosition}");

            kb.Tell($"Visited({nextPosition.Item1},{nextPosition.Item2})");
            kb.MarkSafe(nextPosition.Item1, nextPosition.Item2);
            kb.Tell($"NoPit({nextPosition.Item1},{nextPosition.Item2})");
           
            kb.Tell($"NoWumpus({nextPosition.Item1},{nextPosition.Item2})");

var cellContents = world.GetCell(nextPosition.Item1, nextPosition.Item2);
 if (cellContents.Contains("Pit") || cellContents.Contains("Wumpus"))
        {
            isAlive = false;
            score -= 1000;
            Log($"Agent died at {nextPosition}!");
        }
        else
        {
            PerceiveEnvironment();
        }
        }

        private void UpdateFacing((int, int) nextPosition)
        {
            int dx = nextPosition.Item1 - world.AgentPosition.Item1;
            int dy = nextPosition.Item2 - world.AgentPosition.Item2;

            if (dx > 0)
                facing = Direction.Right;
            else if (dx < 0)
                facing = Direction.Left;
            else if (dy > 0)
                facing = Direction.Up;
            else if (dy < 0)
                facing = Direction.Down;
            // If dx and dy are both 0, we don't change the facing direction
        }

        private int DistanceToGoal((int, int) position)
        {
            return ManhattanDistance(position, world.GoalPosition);
        }

        private int DistanceToAgent((int, int) position)
        {
            return ManhattanDistance(position, world.AgentPosition);
        }

        private int turnCount = 0;

        private void Log(string message, bool incrementTurn = true)
        {
            if (isAlive)
            {
                if (incrementTurn)
                {
                    turnCount++;
                }
                actionLog.Add($"[Turn {turnCount}] {message}");
            }
        }

        private void PrintActionLog()
        {
            Console.WriteLine("\nAction Log:");
            foreach (var action in actionLog)
            {
                Console.WriteLine(action);
            }
        }
    }

    class KnowledgeBase
    {
        private HashSet<string> facts;
        private HashSet<(int, int)> safeCells;
        private HashSet<(int, int)> visitedCells;
        public KnowledgeBase()
        {
            facts = new HashSet<string>();
            safeCells = new HashSet<(int, int)>();
            visitedCells = new HashSet<(int, int)>();
            InitializeKB();
        }

        private void InitializeKB()
        {
            // Add initial axioms
            Tell("all x y (Adjacent(x,y) <-> Adjacent(y,x)).");
            Tell("all x y (Breeze(x,y) <-> exists z (Adjacent(x,y,z) & Pit(z))).");
            Tell("all x y (Stench(x,y) <-> exists z (Adjacent(x,y,z) & Wumpus(z))).");
            Tell("all x y (NoPit(x,y) <-> !Pit(x,y)).");
            Tell("all x y (NoWumpus(x,y) <-> !Wumpus(x,y)).");
        }

        public void Tell(string fact)
        {
            facts.Add(fact);
        }

        public bool Ask(string query)
        {
            return InferFact(query);
        }

        public void Remove(string query)
        {
            facts.Remove(query);
        }

        public void MarkSafe(int x, int y)
        {
            safeCells.Add((x, y));
             Remove($"PossiblePit({x},{y})");
        Remove($"PossibleWumpus({x},{y})");
        }

        public bool IsSafe(int x, int y)
        {
            return safeCells.Contains((x, y)) && 
               !Ask($"Pit({x},{y})") && 
               !Ask($"Wumpus({x},{y})");
        }

        public void MarkVisited(int x, int y)
        {
            visitedCells.Add((x, y));
            MarkSafe(x, y);
        }


        public bool IsVisited(int x, int y)
        {
            return visitedCells.Contains((x, y));
        }

        public void Clear()
        {
            facts.Clear();
            safeCells.Clear();
            visitedCells.Clear();
        }


        private bool InferFact(string query)
        {
            if (query.StartsWith("NoPit"))
            {
                var (x, y) = ParseCoordinates(query);
                return !facts.Contains($"Pit({x},{y})");
            }
            if (query.StartsWith("DeadWumpus"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"DeadWumpus({x},{y})");
            }

            if (query.StartsWith("NoWumpus"))
            {
                var (x, y) = ParseCoordinates(query);
                return !facts.Contains($"Wumpus({x},{y})");
            }
            if (query.StartsWith("PossiblePit"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"PossiblePit({x},{y})");
            }

            if (query.StartsWith("PossibleWumpus"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"PossibleWumpus({x},{y})");
            }

            if (query.StartsWith("Stench"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"Stench({x},{y})");
            }

            if (query.StartsWith("Breeze"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"Breeze({x},{y})");
            }

            if (query.StartsWith("Safe"))
            {
                var (x, y) = ParseCoordinates(query);
                return !facts.Contains($"Pit({x},{y})") && !facts.Contains($"Wumpus({x},{y})");
            }

            if (query.StartsWith("Pit"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"Pit({x},{y})");
            }

            if (query.StartsWith("Wumpus"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"Wumpus({x},{y})");
            }

            if (query.StartsWith("Gold"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"Gold({x},{y})");
            }

            if (query.StartsWith("GoldCollected"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"GoldCollected({x},{y})");
            }

            if (query.StartsWith("Visited"))
            {
                var (x, y) = ParseCoordinates(query);
                return facts.Contains($"Visited({x},{y})");
            }

            return false;
        }

        private (int x, int y) ParseCoordinates(string fact)
        {
            var parts = fact.Split('(', ')', ',');
            return (int.Parse(parts[1]), int.Parse(parts[2]));
        }
    }
}