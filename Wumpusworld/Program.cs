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

    private bool isAlive;
    private int score;
    private bool hasArrow;
    private List<string> actionLog;

    public Agent(WumpusWorld world)
    {
        this.world = world;
        this.kb = new KnowledgeBase();
        this.visitedCells = new List<(int, int)>();
        this.facing = Direction.Right;
         this.isAlive = true;
        this.score = 0;
        this.hasArrow = true;
        this.actionLog = new List<string>();
    }

    public void Play()
    {
        while (isAlive)
        {
            PerceiveEnvironment();
            if (!isAlive)
            {
                Log("Game Over: Agent died!");
                break;
            }
            if (world.AgentPosition == world.GoalPosition)
            {
                Log($"Reached the goal! Final score: {score}");
                break;
            }
            (int, int) nextMove = PlanNextMove();
            Move(nextMove);
        }
        PrintActionLog();
    }

    private void PerceiveEnvironment()
    {
       var perceptions = world.GetCell(world.AgentPosition.Item1, world.AgentPosition.Item2);
        foreach (var perception in perceptions)
        {
            kb.Tell($"{perception}({world.AgentPosition.Item1},{world.AgentPosition.Item2})");
            Log($"{perception} sensed at {world.AgentPosition}");

            if (perception == "Gold")
            {
                score += 1000;
                Log("Gold picked up!");
            }
            else if (perception == "Pit" || perception == "Wumpus")
            {
                isAlive = false;
                score -= 1000;  // Penalty for dying
                Log($"Agent died! Stepped into a {perception}");
                return;  // Stop perceiving if dead
            }
        }
    }

    private (int, int) PlanNextMove()
    {
        var safeMoves = GetSafeMoves();
        if (safeMoves.Count == 0)
        {
            if (hasArrow)
            {
                ShootArrow();
                safeMoves = GetSafeMoves();
            }
            if (safeMoves.Count == 0)
            {
                return GetRiskyMove();
            }
        }

        return FindBestMove(safeMoves);
    }

    private (int, int) FindBestMove(List<(int, int)> safeMoves)
    {
        var unvisitedMoves = safeMoves.Where(m => !visitedCells.Contains(m)).ToList();
        if (unvisitedMoves.Any())
        {
            return unvisitedMoves.OrderBy(DistanceToGoal).First();
        }
        return safeMoves.OrderBy(DistanceToGoal).First();
    }

    private List<(int, int)> GetSafeMoves()
    {
        var possibleMoves = new List<(int, int)>
        {
            (world.AgentPosition.Item1 + 1, world.AgentPosition.Item2),
            (world.AgentPosition.Item1 - 1, world.AgentPosition.Item2),
            (world.AgentPosition.Item1, world.AgentPosition.Item2 + 1),
            (world.AgentPosition.Item1, world.AgentPosition.Item2 - 1)
        };

        return possibleMoves.Where(m => IsValidMove(m) && IsSafe(m)).ToList();
    }

    private bool IsValidMove((int, int) move)
    {
        return move.Item1 >= 1 && move.Item1 <= world.Width &&
               move.Item2 >= 1 && move.Item2 <= world.Height;
    }

    private bool IsSafe((int, int) move)
    {
        return kb.Ask($"Safe({move.Item1},{move.Item2})");
    }

    private (int, int) GetRiskyMove()
    {
        var possibleMoves = GetSafeMoves();
        if (possibleMoves.Count == 0)
        {
            possibleMoves = new List<(int, int)>
            {
                (world.AgentPosition.Item1 + 1, world.AgentPosition.Item2),
                (world.AgentPosition.Item1 - 1, world.AgentPosition.Item2),
                (world.AgentPosition.Item1, world.AgentPosition.Item2 + 1),
                (world.AgentPosition.Item1, world.AgentPosition.Item2 - 1)
            }.Where(IsValidMove).ToList();
        }

        return possibleMoves
            .OrderBy(m => kb.Ask($"Pit({m.Item1},{m.Item2})") ? 1 : 0)
            .ThenBy(m => kb.Ask($"Wumpus({m.Item1},{m.Item2})") ? 1 : 0)
            .ThenBy(DistanceToGoal)
            .First();
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
            kb.Tell($"-Wumpus({target.Item1},{target.Item2})");
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

        // Check if the new position has a pit or Wumpus
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
        int dx = nextPosition.Item1 - world.AgentPosition.Item1;
        int dy = nextPosition.Item2 - world.AgentPosition.Item2;

        if (dx == 1) facing = Direction.Right;
        else if (dx == -1) facing = Direction.Left;
        else if (dy == 1) facing = Direction.Up;
        else if (dy == -1) facing = Direction.Down;
    }

    private int DistanceToGoal((int, int) position)
    {
        return Math.Abs(position.Item1 - world.GoalPosition.Item1) +
               Math.Abs(position.Item2 - world.GoalPosition.Item2);
    }

    private void Log(string message)
    {
        actionLog.Add($"[Turn {actionLog.Count + 1}] {message}");
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
        // Add initial axioms if needed
    }

    public void Tell(string fact)
    {
        facts.Add(fact);
    }

    public bool Ask(string query)
    {
        return facts.Contains(query) || InferFact(query);
    }

    private bool InferFact(string query)
    {
        // Simple inference rules
        if (query.StartsWith("Safe"))
        {
            var (x, y) = ParseCoordinates(query);
            return !facts.Contains($"Pit({x},{y})") && !facts.Contains($"Wumpus({x},{y})");
        }

        if (query.StartsWith("Pit"))
        {
            var (x, y) = ParseCoordinates(query);
            return facts.Any(f => f.StartsWith("Breeze") && IsAdjacent(f, x, y));
        }

        if (query.StartsWith("Wumpus"))
        {
            var (x, y) = ParseCoordinates(query);
            return facts.Any(f => f.StartsWith("Stench") && IsAdjacent(f, x, y));
        }

        return false;
    }

    private (int x, int y) ParseCoordinates(string fact)
    {
        var parts = fact.Split('(', ')', ',');
        return (int.Parse(parts[1]), int.Parse(parts[2]));
    }

    private bool IsAdjacent(string fact, int x, int y)
    {
        var (fx, fy) = ParseCoordinates(fact);
        return Math.Abs(fx - x) + Math.Abs(fy - y) == 1;
    }
    }
}