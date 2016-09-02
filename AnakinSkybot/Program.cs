using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

public class Player
{
    public static void Main()
    {
        IInputContainer inputContainer = new InputContainer(new ConsoleInputGetter());
        IInitialCheckpointGuesser initialCheckpointGuesser = new InitialCheckpointGuesser(inputContainer);
        ICheckpointMemory iCheckpointMemory = new CheckpointMemory();
        IBoostUseCalculator boostUseCalculator = new BoostUseCalculator(iCheckpointMemory);

        new Player().Start(inputContainer, initialCheckpointGuesser, iCheckpointMemory, boostUseCalculator);
    }

    public void Start(IInputContainer inputContainer, IInitialCheckpointGuesser initialCheckpointGuesser,
        ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator)
    {
        bool boost = false;
        int speed = -1;
        Point lastPosition = new Point(-1, -1);

        // Game Loop
        while (true)
        {
            // Update input on start of each round
            inputContainer.Update();

            #region Checkpoint calculations

            // Set first Checkpoint on game start (guessing)
            if (checkpointMemory.KnownCheckpoints.Count == 0)
                checkpointMemory.AddCheckpoint(initialCheckpointGuesser.GuessInitialCheckPoint());

            // Create a new Checkpoint with current target if we don't know all the Checkpoints yet
            if (!checkpointMemory.AllCheckPointsKnown &&
                checkpointMemory.GetCheckpointAtPosition(inputContainer.NextCheckpointLocation) == null)
                checkpointMemory.AddCheckpoint(inputContainer.NextCheckpointLocation);

            // Try to get the current target Checkpoint. If its null, then we add the newCP and set it as current
            // we use a threshold of 600 because we guessed the first Checkpoint
            var currentCp = checkpointMemory.GetCheckpointAtPosition(inputContainer.NextCheckpointLocation);
            if (currentCp == null)
            {
                checkpointMemory.AddCheckpoint(inputContainer.NextCheckpointLocation);
                currentCp = checkpointMemory.GetCheckpointAtPosition(inputContainer.NextCheckpointLocation);
            }

            // if we target the first Checkpoint we can safely say, that we know all Checkpoints
            if (currentCp.Id == 0 &&
                !checkpointMemory.AllCheckPointsKnown)
            {
                // update the first Checkpoint with exact values
                checkpointMemory.UpdateCheckpoint(currentCp, inputContainer.NextCheckpointLocation);

                checkpointMemory.AllCheckPointsKnown = true;
            }

            #endregion

            #region speed calculations

            if (lastPosition.X != -1 &&
                lastPosition.Y != -1)
            {
                speed = Math.Abs(inputContainer.PlayerPosition.X - lastPosition.X) +
                        Math.Abs(inputContainer.PlayerPosition.Y - lastPosition.Y);
            }

            #endregion

            #region target finding

            int nextTargetX = inputContainer.NextCheckpointLocation.X;
            int nextTargetY = inputContainer.NextCheckpointLocation.Y;

            if (checkpointMemory.AllCheckPointsKnown &&
                inputContainer.DistanceToNextCheckPoint < 1500 &&
                speed > 500)
            {
                Console.Error.WriteLine("currentCP: " + currentCp.Id);
                var nextTargetCp = checkpointMemory.KnownCheckpoints.ToList().Find(cp => cp.Id == currentCp.Id + 1);
                if (nextTargetCp == null)
                    nextTargetCp = checkpointMemory.KnownCheckpoints[0];
                Console.Error.WriteLine("nextTargetCP: " + nextTargetCp.Id);

                nextTargetX = nextTargetCp.Position.X;
                nextTargetY = nextTargetCp.Position.Y;
            }

            #endregion

            #region thrust calculations

            // calculate slow down value for distance to next target
            int nextTargetDist = Math.Abs(inputContainer.NextCheckpointLocation.X - inputContainer.PlayerPosition.X) +
                                 Math.Abs(inputContainer.NextCheckpointLocation.Y - inputContainer.PlayerPosition.Y);
            int distSlow = 0;
            if (nextTargetDist < 2000)
                distSlow = (2000 - nextTargetDist) / 20;

            // calculate slow down value for angle to next checkpoint
            int angleSlow = 0;
            if (Math.Abs(inputContainer.AngleToNextCheckPoint) > 10)
                angleSlow = (Math.Abs(inputContainer.AngleToNextCheckPoint) - 10) /** * 10 / 10 **/;

            // calculate slow down value for current vector angle to next checkpoint
            int currentVectorX = inputContainer.NextCheckpointLocation.X - lastPosition.X;
            int currentVectorY = inputContainer.NextCheckpointLocation.Y - lastPosition.Y;
            int nextCpVectorX = inputContainer.NextCheckpointLocation.X - inputContainer.NextCheckpointLocation.X;
            int nextCpVectorY = inputContainer.NextCheckpointLocation.Y - inputContainer.NextCheckpointLocation.Y;

            double currentVectorAngle = Math.Acos((currentVectorX * nextCpVectorX + currentVectorY * nextCpVectorY) /
                                                  Math.Pow(
                                                      (Math.Pow(currentVectorX, 2) + Math.Pow(currentVectorY, 2)) *
                                                      (Math.Pow(nextCpVectorX, 2) + Math.Pow(nextCpVectorY, 2)), 0.5)) *
                                        (180.0 / Math.PI);
            if (currentVectorAngle > 10)
                angleSlow += (int)Math.Round(currentVectorAngle - 10) / 10;

            // calculate thrust
            int thrust = 100 - distSlow - angleSlow;
            if (thrust < 5)
                thrust = 5;

            string sThrust = thrust.ToString();

            // if we pass the boostCP -> BOOOOOST...
            if (checkpointMemory.AllCheckPointsKnown &&
                inputContainer.NextCheckpointLocation == boostUseCalculator.GetBoostTargetCheckpoint().Position &&
                angleSlow == 0 &&
                inputContainer.DistanceToNextCheckPoint > 4000 &&
                !boost)
            {
                sThrust = "BOOST";
                boost = true;
            }

            #endregion

            #region status messages

            if (true)
            {
                foreach (var cp in checkpointMemory.KnownCheckpoints)
                    Console.Error.WriteLine(cp.Id + " " + cp.Position.X + " " + cp.Position.Y + " " + cp.DistToNext);

                Console.Error.WriteLine("cp count: " + checkpointMemory.KnownCheckpoints.Count);

                Console.Error.WriteLine("allCheckpointsKnown: " + checkpointMemory.AllCheckPointsKnown);

                Console.Error.WriteLine("nextCheckpointDist: " + inputContainer.DistanceToNextCheckPoint);

                Console.Error.WriteLine("nextCheckpointAngle: " + inputContainer.AngleToNextCheckPoint);

                Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

                Console.Error.WriteLine("currentVectorX: " + currentVectorX);
                Console.Error.WriteLine("currentVectorY: " + currentVectorY);

                Console.Error.WriteLine("nextCpVectorX: " + nextCpVectorX);
                Console.Error.WriteLine("nextCpVectorY: " + nextCpVectorY);

                Console.Error.WriteLine("lastPosition: " + lastPosition);
                Console.Error.WriteLine("currentPosition: " + inputContainer.PlayerPosition);

                Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);
                Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

                Console.Error.WriteLine("distSlow: " + distSlow);

                Console.Error.WriteLine("angleSlow: " + angleSlow);

                Console.Error.WriteLine("boostCpId: " + boostUseCalculator.GetBoostTargetCheckpoint().Id);

                Console.Error.WriteLine("thrust: " + thrust);
            }

            #endregion

            lastPosition = inputContainer.PlayerPosition;

            Console.WriteLine(nextTargetX + " " + nextTargetY + " " + sThrust);
        }
    }
}

public class Checkpoint
{
    public int Id { get; private set; }

    public Point Position { get; private set; }

    public int DistToNext { get; set; }

    public Checkpoint(int id, int x, int y)
    {
        Id = id;
        Position = new Point(x, y);
    }

    public Checkpoint(int id, Point position) : this(id, position.X, position.Y)
    {}
}

public interface IInputContainer
{
    int AngleToNextCheckPoint { get; }
    int DistanceToNextCheckPoint { get; }
    Point NextCheckpointLocation { get; }
    Point OpponentPosition { get; }
    Point PlayerPosition { get; }

    void Update();
}

public class InputContainer : IInputContainer
{
    private readonly IInputGetter inputGetter;
    public Point PlayerPosition { get; private set; }

    public Point NextCheckpointLocation { get; private set; }

    public int DistanceToNextCheckPoint { get; private set; }

    public int AngleToNextCheckPoint { get; private set; }

    public Point OpponentPosition { get; private set; }

    public InputContainer(IInputGetter inputGetter)
    {
        this.inputGetter = inputGetter;
    }

    public void Update()
    {
        string[] inputs = inputGetter.GetInput();

        PlayerPosition = new Point(int.Parse(inputs[0]), int.Parse(inputs[1]));
        NextCheckpointLocation = new Point(int.Parse(inputs[2]), int.Parse(inputs[3]));
        DistanceToNextCheckPoint = int.Parse(inputs[4]);
        AngleToNextCheckPoint = int.Parse(inputs[5]);

        inputs = inputGetter.GetInput();
        OpponentPosition = new Point(int.Parse(inputs[0]), int.Parse(inputs[1]));
    }
}

public interface IInputGetter
{
    string[] GetInput();
}

public class ConsoleInputGetter : IInputGetter
{
    public string[] GetInput()
    {
        return Console.ReadLine().Split(' ');
    }
}

public interface IInitialCheckpointGuesser
{
    Checkpoint GuessInitialCheckPoint();
}

public class InitialCheckpointGuesser : IInitialCheckpointGuesser
{
    private readonly IInputContainer inputContainer;

    public InitialCheckpointGuesser(IInputContainer inputContainer)
    {
        this.inputContainer = inputContainer;
    }

    #region Implementation of IInitialCheckpointGuesser

    public Checkpoint GuessInitialCheckPoint()
    {
        // We guess that the first/final Checkpoint is in between our pod and the opponent
        int diffX = inputContainer.OpponentPosition.X - inputContainer.PlayerPosition.X;
        int diffY = inputContainer.OpponentPosition.Y - inputContainer.PlayerPosition.Y;

        var initialCp = new Checkpoint(0, inputContainer.PlayerPosition.X + diffX,
            inputContainer.PlayerPosition.Y + diffY);
        return initialCp;
    }

    #endregion
}

public interface ICheckpointMemory
{
    bool AllCheckPointsKnown { get; set; }
    Checkpoint CurrentCheckpoint { get; set; }
    ReadOnlyCollection<Checkpoint> KnownCheckpoints { get; }

    void AddCheckpoint(Point p);
    void AddCheckpoint(Checkpoint checkpoint);
    Checkpoint GetCheckpointAtPosition(Point p, int threshold = 600);
    void UpdateCheckpoint(Checkpoint checkpoint, Point newPosition);
}

public class CheckpointMemory : ICheckpointMemory
{
    private readonly List<Checkpoint> knownCheckpoints;

    private int CheckpointCounter
    {
        get
        {
            return knownCheckpoints.Count;
        }
    }

    public ReadOnlyCollection<Checkpoint> KnownCheckpoints => knownCheckpoints.AsReadOnly();

    public bool AllCheckPointsKnown { get; set; }

    public Checkpoint CurrentCheckpoint { get; set; }

    public CheckpointMemory()
    {
        knownCheckpoints = new List<Checkpoint>();
    }

    public void AddCheckpoint(Checkpoint checkpoint)
    {
        knownCheckpoints.Add(checkpoint);
    }

    public void AddCheckpoint(Point p)
    {
        AddCheckpoint(new Checkpoint(CheckpointCounter, p.X, p.Y));
    }

    public Checkpoint GetCheckpointAtPosition(Point p, int threshold = 600)
    {
        return
            knownCheckpoints.Find(
                cp =>
                    (cp.Position.X >= p.X - threshold && cp.Position.X <= p.X + threshold) &&
                    (cp.Position.Y >= p.Y - threshold && cp.Position.Y <= p.Y + threshold));
    }

    public void UpdateCheckpoint(Checkpoint checkpoint, Point newPosition)
    {
        knownCheckpoints[checkpoint.Id] = new Checkpoint(checkpoint.Id, newPosition);
    }
}

public interface IBoostUseCalculator
{
    Checkpoint GetBoostTargetCheckpoint();
}

public class BoostUseCalculator : IBoostUseCalculator
{
    private readonly ICheckpointMemory checkpointMemory;

    public BoostUseCalculator(ICheckpointMemory checkpointMemory)
    {
        this.checkpointMemory = checkpointMemory;
    }

    public Checkpoint GetBoostTargetCheckpoint()
    {
        // calculate distance to next Checkpoint for every Checkpoint
        foreach (var cp in checkpointMemory.KnownCheckpoints)
        {
            var cpNext = checkpointMemory.KnownCheckpoints.ToList().Find(ncp => ncp.Id == cp.Id + 1);
            if (cpNext == null)
                cpNext = checkpointMemory.KnownCheckpoints[0];

            int distX = cpNext.Position.X - cp.Position.X;
            int distY = cpNext.Position.Y - cp.Position.Y;

            cp.DistToNext = Math.Abs(distX) + Math.Abs(distY);
        }

        // find Checkpoint with longest distance to next Checkpoint
        int boostCpId = checkpointMemory.KnownCheckpoints.ToList().OrderByDescending(item => item.DistToNext).First().Id;

        // find and return next Checkpoint (the Checkpoint to boost to)
        Checkpoint boostCheckpoint = checkpointMemory.KnownCheckpoints.ToList().Find(x => x.Id == boostCpId + 1);
        return boostCheckpoint ?? checkpointMemory.KnownCheckpoints[0];
    }
}