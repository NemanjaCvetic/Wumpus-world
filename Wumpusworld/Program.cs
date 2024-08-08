using System.Diagnostics;
using System.Text;

internal class Program
{



    private static void Main(string[] args)
    {

        Console.WriteLine("Welcome to the Wumpus World!");

        while (true)
        {
            Console.Write("Enter the filename for the Wumpus World (or 'quit' to exit): ");
            string filename = Console.ReadLine();

            if (filename.ToLower() == "quit")
                break;

            try
            {
                WumpusWorld world = new WumpusWorld(filename);
                Agent agent = new Agent(world);

                Console.WriteLine($"Starting the game with world from {filename}...");
                agent.Play();

                Console.WriteLine("Game over!");
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

        public WumpusWorld(string filename)
        {
            world = new Dictionary<(int, int), List<string>>();
            ParseWorldFile(filename);
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

        public Agent(WumpusWorld world)
        {
            this.world = world;
            this.kb = new KnowledgeBase();
            this.visitedCells = new List<(int, int)>();
            this.facing = Direction.Right;
            this.score = 0;
            this.hasArrow = true;
            this.actionLog = new List<string>();
            this.isAlive = true;
            this.plannedPath = new Queue<(int, int)>();
        }

        public void Play()
        {
            while (isAlive)
    {
        (int, int) nextMove = PlanNextMove();
        Move(nextMove);

        PerceiveEnvironment();
        if (!isAlive) break;

        if (world.AgentPosition == world.GoalPosition)
        {
            Log($"Reached the goal! Final score: {score}");
            break;
        }
    }

    PrintActionLog();
        }

       private void PerceiveEnvironment()
{
    var perceptions = world.GetCell(world.AgentPosition.Item1, world.AgentPosition.Item2);
    foreach (var perception in perceptions)
    {
        HandlePerception(perception);
    }
}

        private void HandlePerception(string perception)
{
    switch (perception)
    {
        case "Breeze":
            kb.Tell($"Breeze({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
            Log($"Breeze sensed at {world.AgentPosition}");
            UpdateAdjacentCellsKnowledge();
            break;
        case "Smell":
            kb.Tell($"Stench({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
            Log($"Smell sensed at {world.AgentPosition}");
            DeduceWumpusLocation();
            break;
        case "Gold":
            kb.Tell($"Gold({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
            Log($"Gold found at {world.AgentPosition}");
            score += 1000;
            break;
        case "Pit":
            kb.Tell($"Pit({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
            Log($"Found out pit is on field {world.AgentPosition}");
            isAlive = false;
            score -= 1000;
            Log($"Agent died! Stepped into a Pit");
            break;
        case "Wumpus":
            kb.Tell($"Wumpus({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
            Log($"Found out Wumpus is on field {world.AgentPosition}");
            if (hasArrow && !kb.Ask($"Wumpus({world.AgentPosition.Item1},{world.AgentPosition.Item2})"))
            {
                ShootArrow();
            }
            else
            {
                isAlive = false;
                score -= 1000;
                Log($"Agent died! Stepped into a Wumpus");
            }
            break;
    }
}

     private void DeduceWumpusLocation()
{
    for (int x = 1; x <= world.Width; x++)
    {
        for (int y = 1; y <= world.Height; y++)
        {
            if (kb.Ask($"Stench({x},{y})") && !kb.Ask($"Wumpus({x},{y})"))
            {
                kb.Tell($"Wumpus({x},{y})");
                Log($"Deduced that the Wumpus is on field ({x},{y})");
            }
        }
    }
}




       private void UpdateAdjacentCellsKnowledge()
{
    var adjacentCells = GetAdjacentCells(world.AgentPosition);
    foreach (var cell in adjacentCells)
    {
        if (!kb.Ask($"Visited({cell.Item1},{cell.Item2})"))
        {
            if (!kb.Ask($"Breeze({world.AgentPosition.Item1},{world.AgentPosition.Item2})"))
            {
                kb.Tell($"NoPit({cell.Item1},{cell.Item2})");
            }
            if (!kb.Ask($"Stench({world.AgentPosition.Item1},{world.AgentPosition.Item2})"))
            {
                kb.Tell($"NoWumpus({cell.Item1},{cell.Item2})");
            }
        }
    }
}

       private (int, int) PlanNextMove()
{
    if (plannedPath.Count == 0)
    {
        plannedPath = PlanPath();
    }

    if (plannedPath.Count > 0)
    {
        var nextMove = plannedPath.Dequeue();
        if (kb.Ask($"Wumpus({nextMove.Item1},{nextMove.Item2})") || kb.Ask($"Pit({nextMove.Item1},{nextMove.Item2})"))
        {
            if (hasArrow && kb.Ask($"Wumpus({nextMove.Item1},{nextMove.Item2})"))
            {
                ShootArrow();
                return world.AgentPosition;  // Stay in place after shooting
            }
            else
            {
                isAlive = false;
                score -= 1000;
                if (kb.Ask($"Wumpus({nextMove.Item1},{nextMove.Item2})"))
                {
                    Log($"Agent died! Stepped into a Wumpus");
                }
                else
                {
                    Log($"Agent died! Stepped into a Pit");
                }
                return world.AgentPosition;
            }
        }
        else
        {
            Log($"Move to field {nextMove}");
            return nextMove;
        }
    }

    var safestMove = GetSafestMove();
    Log($"Move to field {safestMove}");
    return safestMove;
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
                    if (!visitedCells.Contains((x, y)) && IsSafe((x, y)))
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

                if (current == goal)
                {
                    break;
                }

                foreach (var next in GetAdjacentCells(current))
                {
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
    var safeCells = adjacentCells.Where(IsSafe).ToList();

    if (safeCells.Any())
    {
        return safeCells.OrderBy(DistanceToGoal).First();
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
    if (kb.Ask($"Pit({cell.Item1},{cell.Item2})")) danger += 10;
    if (kb.Ask($"Wumpus({cell.Item1},{cell.Item2})")) danger += 100;
    return danger;
}

       private bool IsSafe((int, int) cell)
{
    return kb.Ask($"NoPit({cell.Item1},{cell.Item2})") &&
           kb.Ask($"NoWumpus({cell.Item1},{cell.Item2})");
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
            hasArrow = false;
            score -= 100;
            Log("Arrow shot!");

            (int, int) target = GetTargetCell();
            if (kb.Ask($"Wumpus({target.Item1},{target.Item2})"))
            {
                Log("Wumpus killed!");
                kb.Tell($"NoWumpus({target.Item1},{target.Item2})");
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
            UpdateFacing(nextPosition);
            world.AgentPosition = nextPosition;
            score -= 1; // Cost of moving
            visitedCells.Add(nextPosition);
            Log($"Moved to {nextPosition}");

            kb.Tell($"Visited({nextPosition.Item1},{nextPosition.Item2})");

            var dangers = world.GetCell(nextPosition.Item1, nextPosition.Item2)
                .Where(p => p == "Pit" || p == "Wumpus")
                .ToList();

            if (dangers.Any())
            {
                isAlive = false;
                score -= 1000;  // Penalty for dying
                Log($"Agent died! Stepped into a {dangers.First()}");
            }
        }

        private void UpdateFacing((int, int) nextPosition)
        {
            // ... (existing implementation)
        }

        private int DistanceToGoal((int, int) position)
        {
            return ManhattanDistance(position, world.GoalPosition);
        }

        private int DistanceToAgent((int, int) position)
        {
            return ManhattanDistance(position, world.AgentPosition);
        }

        private void Log(string message)
        {
           if (isAlive)
    {
        actionLog.Add($"[Turn {actionLog.Count + 1}] {message}");
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

        public KnowledgeBase()
        {
            facts = new HashSet<string>();
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

        private bool InferFact(string query)
        {
            if (query.StartsWith("NoPit"))
            {
                var (x, y) = ParseCoordinates(query);
                return !facts.Contains($"Pit({x},{y})");
            }

            if (query.StartsWith("NoWumpus"))
            {
                var (x, y) = ParseCoordinates(query);
                return !facts.Contains($"Wumpus({x},{y})");
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