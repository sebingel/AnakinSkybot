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
        ICheckpointMemory checkpointMemory = new CheckpointMemory();
        IBoostUseCalculator boostUseCalculator = new BoostUseCalculator(checkpointMemory);
        ISpeedCalculator speedCalculator = new SpeedCalculator();
        ITargetFinding targetFinding = new TargetFinder(checkpointMemory, inputContainer, speedCalculator);
        IThrustCalculator thrustCalculator = new ThrustCalculator(checkpointMemory,boostUseCalculator);

        new Player().Start(inputContainer, initialCheckpointGuesser, checkpointMemory, boostUseCalculator,
            speedCalculator, targetFinding, thrustCalculator);
    }

    public void Start(IInputContainer inputContainer, IInitialCheckpointGuesser initialCheckpointGuesser,
        ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator, ISpeedCalculator speedCalculator,
        ITargetFinding targetFinding, IThrustCalculator thrustCalculator)
    {
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

            Checkpoint nextCp = null;
            Checkpoint cpAfterNextCp = null;
            if (checkpointMemory.AllCheckPointsKnown)
            {
                nextCp = checkpointMemory.KnownCheckpoints.ToList().Find(x => x.Id == currentCp.Id + 1) ??
                         checkpointMemory.KnownCheckpoints[0];

                cpAfterNextCp = checkpointMemory.KnownCheckpoints.ToList().Find(x => x.Id == nextCp.Id + 1) ??
                                checkpointMemory.KnownCheckpoints[0];
            }

            #endregion

            #region speed calculations

            //int speed = speedCalculator.GetSpeed(inputContainer.PlayerPosition);

            #endregion

            #region target finding

            Point target = targetFinding.GetTarget(inputContainer.PlayerPosition, currentCp, nextCp);

            #endregion

            #region thrust calculations

            string sThrust =
                thrustCalculator.GetThrust(inputContainer.DistanceToNextCheckPoint, inputContainer.AngleToNextCheckPoint,
                    inputContainer.PlayerPosition, nextCp?.Position);

            #endregion

            #region status messages

            if (false)
            {
                foreach (var cp in checkpointMemory.KnownCheckpoints)
                    Console.Error.WriteLine(cp.Id + " " + cp.Position.X + " " + cp.Position.Y + " " + cp.DistToNext);

                Console.Error.WriteLine("cp count: " + checkpointMemory.KnownCheckpoints.Count);

                Console.Error.WriteLine("allCheckpointsKnown: " + checkpointMemory.AllCheckPointsKnown);

                Console.Error.WriteLine("nextCheckpointDist: " + inputContainer.DistanceToNextCheckPoint);

                Console.Error.WriteLine("nextCheckpointAngle: " + inputContainer.AngleToNextCheckPoint);

                //Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

                //Console.Error.WriteLine("currentVectorX: " + currentVectorX);
                //Console.Error.WriteLine("currentVectorY: " + currentVectorY);

                //Console.Error.WriteLine("nextCpVectorX: " + nextCpVectorX);
                //Console.Error.WriteLine("nextCpVectorY: " + nextCpVectorY);

                //Console.Error.WriteLine("lastPosition: " + lastPosition);
                //Console.Error.WriteLine("currentPosition: " + inputContainer.PlayerPosition);

                //Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);
                //Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

                //Console.Error.WriteLine("distSlow: " + distSlow);

                //Console.Error.WriteLine("angleSlow: " + angleSlow);

                Console.Error.WriteLine("boostCpId: " + boostUseCalculator.GetBoostTargetCheckpoint().Id);

                //Console.Error.WriteLine("thrust: " + thrust);

                Console.Error.WriteLine("currentCP: " + currentCp.Id);

                Console.Error.WriteLine("nextCP: " + nextCp?.Id);
            }

            #endregion

            Console.WriteLine(target.X + " " + target.Y + " " + sThrust);
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

public interface ISpeedCalculator
{
    int GetSpeed(Point currentPosition);
}

public class SpeedCalculator : ISpeedCalculator
{
    private Point lastPosition;

    public int GetSpeed(Point currentPosition)
    {
        int speed = Math.Abs(currentPosition.X - lastPosition.X) + Math.Abs(currentPosition.Y - lastPosition.Y);

        lastPosition = currentPosition;

        return speed;
    }
}

public interface ITargetFinding
{
    Point GetTarget(Point currentPosition, Checkpoint currentCp, Checkpoint nextCp);
}

public class TargetFinder : ITargetFinding
{
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IInputContainer inputContainer;
    private readonly ISpeedCalculator speed;

    public TargetFinder(ICheckpointMemory checkpointMemory, IInputContainer inputContainer, ISpeedCalculator speed)
    {
        this.checkpointMemory = checkpointMemory;
        this.inputContainer = inputContainer;
        this.speed = speed;
    }

    public Point GetTarget(Point currentPosition, Checkpoint currentCp, Checkpoint nextCp)
    {
        Point target = currentCp.Position;

        if (checkpointMemory.AllCheckPointsKnown &&
            inputContainer.DistanceToNextCheckPoint < 1500 &&
            speed.GetSpeed(currentPosition) > 500)
            target = nextCp.Position;

        return target;
    }
}

public interface IThrustCalculator
{
    string GetThrust(int distanceToNextCheckpoint, int angleToNextCheckpoint, Point playerPosition,
        Point? nextCheckpointLocation);
}

public class ThrustCalculator : IThrustCalculator
{
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IBoostUseCalculator boostUseCalculator;
    private Point? lastPosition;
    private bool boost;

    public ThrustCalculator(ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator)
    {
        this.checkpointMemory = checkpointMemory;
        this.boostUseCalculator = boostUseCalculator;
        boost = false;
    }

    public string GetThrust(int distanceToNextCheckpoint, int angleToNextCheckpoint, Point playerPosition,
        Point? nextCheckpointLocation)
    {
        // calculate slow down value for distance to next target
        int distSlow = 0;
        if (distanceToNextCheckpoint < 2000)
            distSlow = (2000 - distanceToNextCheckpoint) / 20;

        // calculate slow down value for angle to next checkpoint
        int angleSlow = 0;
        if (Math.Abs(angleToNextCheckpoint) > 10)
            angleSlow = (Math.Abs(angleToNextCheckpoint) - 10) /** * 10 / 10 **/;

        if (lastPosition != null &&
            nextCheckpointLocation != null)
        {
            // calculate slow down value for current vector angle to next checkpointnex
            int currentVectorX = playerPosition.X - lastPosition.Value.X;
            int currentVectorY = playerPosition.Y - lastPosition.Value.Y;
            int nextCpVectorX = playerPosition.X - nextCheckpointLocation.Value.X;
            int nextCpVectorY = playerPosition.Y - nextCheckpointLocation.Value.Y;

            double currentVectorAngle = Math.Acos((currentVectorX * nextCpVectorX + currentVectorY * nextCpVectorY) /
                                                  Math.Pow(
                                                      (Math.Pow(currentVectorX, 2) + Math.Pow(currentVectorY, 2)) *
                                                      (Math.Pow(nextCpVectorX, 2) + Math.Pow(nextCpVectorY, 2)), 0.5)) *
                                        (180.0 / Math.PI);
            if (currentVectorAngle > 10)
                angleSlow += (int)Math.Round(currentVectorAngle - 10) / 10;
        }

        // calculate thrust
        int thrust = 100 - distSlow - angleSlow;
        if (thrust < 5)
            thrust = 5;

        string sThrust = thrust.ToString();

        // if we pass the boostCP -> BOOOOOST...
        if (checkpointMemory.AllCheckPointsKnown &&
            nextCheckpointLocation == boostUseCalculator.GetBoostTargetCheckpoint().Position &&
            angleSlow == 0 &&
            distanceToNextCheckpoint > 4000 &&
            !boost)
        {
            sThrust = "BOOST";
            boost = true;
        }

        lastPosition = playerPosition;

        return sThrust;
    }
}