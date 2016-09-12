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
        IAngleCalculator angleCalculator = new AngleCalculator();

        IGamestateCalculator gamestateCalculator = new GamestateCalculator(inputContainer);

        ITargetFinding targetFinding = new SmartTargetFinder(checkpointMemory, inputContainer, speedCalculator,
            angleCalculator);
        //ITargetFinding targetFinding = new SimpleTargetFinder();

        ISlowDownCalculator linearMovementAngleSlowDownCalculator = new LinearMovementAngleSlowDownCalculator(null,
            gamestateCalculator, inputContainer, angleCalculator, checkpointMemory);
        ISlowDownCalculator distanceSlowDownCalculator =
            new SimpleDistanceSlowDownCalculator(linearMovementAngleSlowDownCalculator, checkpointMemory,
                inputContainer);
        ISlowDownCalculator hitPredictionSlowDownCalculator =
            new HitPredictionSlowDownCalculator(distanceSlowDownCalculator, gamestateCalculator, checkpointMemory,
                inputContainer);
        ISlowDownCalculator slowDownCalculator = new LinearAngleSlowDownCalculator(hitPredictionSlowDownCalculator,
            inputContainer);
        //ISlowDownCalculator slowDownCalculator = new SimpleAngleSlowDownCalculator(null,inputContainer);

        IThrustCalculator thrustCalculator = new AngleAndDistThrustCalculator(checkpointMemory, boostUseCalculator,
            angleCalculator,
            slowDownCalculator, gamestateCalculator);

        new Player().Start(inputContainer, initialCheckpointGuesser, checkpointMemory, boostUseCalculator, targetFinding,
            thrustCalculator, speedCalculator, gamestateCalculator);
    }

    public void Start(IInputContainer inputContainer, IInitialCheckpointGuesser initialCheckpointGuesser,
        ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator,
        ITargetFinding targetFinding, IThrustCalculator thrustCalculator, ISpeedCalculator speedCalculator,
        IGamestateCalculator gamestateCalculator)
    {
        // Game Loop
        while (true)
        {
            // Update input on start of each round
            inputContainer.Update();

            gamestateCalculator.Recalculate();

            speedCalculator.CalculateSpeed(inputContainer.PlayerPosition);

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
            checkpointMemory.CurrentCheckpoint =
                checkpointMemory.GetCheckpointAtPosition(inputContainer.NextCheckpointLocation);
            if (checkpointMemory.CurrentCheckpoint == null)
            {
                checkpointMemory.AddCheckpoint(inputContainer.NextCheckpointLocation);
                checkpointMemory.CurrentCheckpoint =
                    checkpointMemory.GetCheckpointAtPosition(inputContainer.NextCheckpointLocation);
            }

            // if we target the first Checkpoint we can safely say, that we know all Checkpoints
            if (checkpointMemory.CurrentCheckpoint.Id == 0 &&
                !checkpointMemory.AllCheckPointsKnown)
            {
                // update the first Checkpoint with exact values
                checkpointMemory.UpdateCheckpoint(checkpointMemory.CurrentCheckpoint,
                    inputContainer.NextCheckpointLocation);

                checkpointMemory.AllCheckPointsKnown = true;
            }

            Checkpoint cpAfterNextCp = null;
            if (checkpointMemory.AllCheckPointsKnown)
            {
                checkpointMemory.NextCheckpoint =
                    checkpointMemory.KnownCheckpoints.ToList()
                        .Find(x => x.Id == checkpointMemory.CurrentCheckpoint.Id + 1) ??
                    checkpointMemory.KnownCheckpoints[0];

                cpAfterNextCp =
                    checkpointMemory.KnownCheckpoints.ToList().Find(x => x.Id == checkpointMemory.NextCheckpoint.Id + 1) ??
                    checkpointMemory.KnownCheckpoints[0];
            }

            #endregion

            #region target finding

            Point target = targetFinding.GetTarget(inputContainer.PlayerPosition, checkpointMemory.CurrentCheckpoint,
                checkpointMemory.NextCheckpoint, cpAfterNextCp);

            #endregion

            #region thrust calculations

            string sThrust =
                thrustCalculator.GetThrust(inputContainer.DistanceToNextCheckPoint, inputContainer.AngleToNextCheckPoint,
                    inputContainer.PlayerPosition, target);

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

                Console.Error.WriteLine("currentCP: " + checkpointMemory.CurrentCheckpoint.Id);

                Console.Error.WriteLine("nextCP: " + checkpointMemory.NextCheckpoint?.Id);
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

public class Vector
{
    public int X { get; }

    public int Y { get; }

    public int Length => (int)Math.Round(Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2)));

    public Vector(int x, int y)
    {
        X = x;
        Y = y;
    }

    #region Overrides of Object

    /// <summary>
    /// Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.
    /// </summary>
    /// <returns>
    /// Eine Zeichenfolge, die das aktuelle Objekt darstellt.
    /// </returns>
    public override string ToString()
    {
        return $"{X}:{Y}";
    }

    #endregion
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

    Checkpoint NextCheckpoint { get; set; }

    ReadOnlyCollection<Checkpoint> KnownCheckpoints { get; }

    void AddCheckpoint(Point p);

    void AddCheckpoint(Checkpoint checkpoint);

    Checkpoint GetCheckpointAtPosition(Point p, int threshold = 600);

    void UpdateCheckpoint(Checkpoint checkpoint, Point newPosition);
}

public class CheckpointMemory : ICheckpointMemory
{
    private readonly List<Checkpoint> knownCheckpoints;
    private bool allCheckPointsKnown;

    private int CheckpointCounter
    {
        get
        {
            return knownCheckpoints.Count;
        }
    }

    public ReadOnlyCollection<Checkpoint> KnownCheckpoints => knownCheckpoints.AsReadOnly();

    public bool AllCheckPointsKnown
    {
        get
        {
            return allCheckPointsKnown;
        }
        set
        {
            allCheckPointsKnown = value;

            // calculate distance to next Checkpoint for every Checkpoint
            foreach (var cp in KnownCheckpoints)
            {
                var cpNext = KnownCheckpoints.ToList().Find(ncp => ncp.Id == cp.Id + 1);
                if (cpNext == null)
                    cpNext = KnownCheckpoints[0];

                int distX = cpNext.Position.X - cp.Position.X;
                int distY = cpNext.Position.Y - cp.Position.Y;

                cp.DistToNext = Math.Abs(distX) + Math.Abs(distY);
                //Console.Error.WriteLine(cp.Id + " " + cp.DistToNext);
            }
        }
    }

    public Checkpoint CurrentCheckpoint { get; set; }

    public Checkpoint NextCheckpoint { get; set; }

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
        //Console.Error.WriteLine(p);
        //knownCheckpoints.ForEach(x => Console.Error.WriteLine(x.Id + " " + x.Position));
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
        // find Checkpoint with longest distance to next Checkpoint
        int boostCpId = checkpointMemory.KnownCheckpoints.ToList().OrderByDescending(item => item.DistToNext).First().Id;
        //Console.Error.WriteLine("boostCpId: " + boostCpId);

        // find and return next Checkpoint (the Checkpoint to boost to)
        Checkpoint boostCheckpoint = checkpointMemory.KnownCheckpoints.ToList().Find(x => x.Id == boostCpId + 1);
        //Console.Error.WriteLine("boostCheckpoint.Id: " + boostCheckpoint?.Id);
        return boostCheckpoint ?? checkpointMemory.KnownCheckpoints[0];
    }
}

public interface ISpeedCalculator
{
    void CalculateSpeed(Point currentPosition);

    int Speed { get; }
}

public class SpeedCalculator : ISpeedCalculator
{
    private Point lastPosition;

    public void CalculateSpeed(Point currentPosition)
    {
        Speed = new Vector(currentPosition.X - lastPosition.X, currentPosition.Y - lastPosition.Y).Length;
        lastPosition = currentPosition;
    }

    public int Speed { get; private set; }
}

public interface ITargetFinding
{
    Point GetTarget(Point currentPosition, Checkpoint currentCp, Checkpoint nextCp,
        Checkpoint checkpointAftertNextCheckpoint);
}

public class SimpleTargetFinder : ITargetFinding
{
    #region Implementation of ITargetFinding

    public Point GetTarget(Point currentPosition, Checkpoint currentCp, Checkpoint nextCp,
        Checkpoint checkpointAftertNextCheckpoint)
    {
        return currentCp.Position;
    }

    #endregion
}

public class SmartTargetFinder : ITargetFinding
{
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IInputContainer inputContainer;
    private readonly ISpeedCalculator speed;
    private readonly IAngleCalculator angleCalculator;

    public SmartTargetFinder(ICheckpointMemory checkpointMemory, IInputContainer inputContainer, ISpeedCalculator speed,
        IAngleCalculator angleCalculator)
    {
        this.checkpointMemory = checkpointMemory;
        this.inputContainer = inputContainer;
        this.speed = speed;
        this.angleCalculator = angleCalculator;
    }

    public Point GetTarget(Point currentPosition, Checkpoint currentCp, Checkpoint nextCp,
        Checkpoint checkpointAftertNextCheckpoint)
    {
        // default target is currentCp
        Point target = currentCp.Position;

        // If we know all Checkpoints we can calculate alternative targets
        if (checkpointMemory.AllCheckPointsKnown)
        {
            if (inputContainer.DistanceToNextCheckPoint < 1500 &&
                speed.Speed > 500)
            {
                //Console.Error.WriteLine("Target next Checkpoint");
                target = GetDesiredPoint(currentCp.Position, nextCp.Position, checkpointAftertNextCheckpoint.Position);
            }
            else
            {
                //Console.Error.WriteLine("Target current Checkpoint");
                target = GetDesiredPoint(currentPosition, currentCp.Position, nextCp.Position);
            }
        }

        return target;
    }

    private Point GetDesiredPoint(Point p1, Point p2, Point p3)
    {
        // Get the vector from p1 to p2
        Vector vecToNextCp = new Vector(p2.X - p1.X, p2.Y - p1.Y);

        // and the vector from p2 to p3
        Vector vecFromNextToFollowingCp = new Vector(p3.X - p2.X, p3.Y - p2.Y);

        // Calculate the angle between these two vectors
        double angle = angleCalculator.CalculateAngle(vecToNextCp, vecFromNextToFollowingCp);

        // split this angle in half
        double halfAngle = angle / 2;

        // convert it into RAD
        double angleInRad = halfAngle * (Math.PI / 180);

        // calculate directional vector to bisected angle
        double cos = Math.Cos(angleInRad);
        double sin = Math.Sin(angleInRad);

        // calculate factor to 300 units away from center
        double fac1 = 300 / cos;
        double fac2 = 300 / sin;
        double factor = Math.Abs(fac1) <= Math.Abs(fac2) ? fac1 : fac2;

        // calculate vector from directional vector and factor (width)
        double x = cos * factor;
        double y = sin * factor;

        // Check x and y of vector for positive/negative values and adjust accordingly
        // with this operation we want to find the optimal point to aim at
        if ((vecFromNextToFollowingCp.X > 0 && x < 0) ||
            (vecFromNextToFollowingCp.X < 0 && x > 0))
            x *= -1;
        if ((vecFromNextToFollowingCp.Y > 0 && y < 0) ||
            (vecFromNextToFollowingCp.Y < 0 && y > 0))
            y *= -1;

        // create vector to desired point from currentCp
        int roundX = (int)Math.Round(x);
        int roundY = (int)Math.Round(y);
        Vector v = new Vector(roundX, roundY);

        //Console.Error.WriteLine(v);

        // if we are further away from the checkpoint than one third of the distance to the last checkpoint...
        Checkpoint currentCheckpoint = checkpointMemory.GetCheckpointAtPosition(new Point(p2.X, p2.Y));
        Checkpoint lastCheckpoint =
            checkpointMemory.KnownCheckpoints.ToList().Find(a => a.Id == currentCheckpoint.Id - 1) ??
            checkpointMemory.KnownCheckpoints[checkpointMemory.KnownCheckpoints.Count - 1];
        //Console.Error.WriteLine($"{inputContainer.DistanceToNextCheckPoint} > {lastCheckpoint.DistToNext / 3} ?");
        if (inputContainer.DistanceToNextCheckPoint >
            lastCheckpoint.DistToNext / 3)
        {
            // ... we aim at the opposite side
            v = new Vector(-roundX, -roundY);
            //Console.Error.WriteLine($"opposite side: {v}");
        }

        return new Point(p2.X + v.X, p2.Y + v.Y);
    }
}

public interface IThrustCalculator
{
    string GetThrust(int distanceToNextCheckpoint, int angleToNextCheckpoint, Point playerPosition,
        Point targetPosition);
}

public class AngleAndDistThrustCalculator : IThrustCalculator
{
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IBoostUseCalculator boostUseCalculator;
    private readonly IAngleCalculator angleCalculator;
    private readonly ISlowDownCalculator slowDownCalculator;
    private readonly IGamestateCalculator gamestateCalculator;
    private bool boost;

    public AngleAndDistThrustCalculator(ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator,
        IAngleCalculator angleCalculator,
        ISlowDownCalculator slowDownCalculator, IGamestateCalculator gamestateCalculator)
    {
        this.checkpointMemory = checkpointMemory;
        this.boostUseCalculator = boostUseCalculator;
        this.angleCalculator = angleCalculator;
        this.slowDownCalculator = slowDownCalculator;
        this.gamestateCalculator = gamestateCalculator;
        boost = false;
    }

    public string GetThrust(int distanceToNextCheckpoint, int angleToNextCheckpoint, Point playerPosition,
        Point targetPosition)
    {
        Vector targetVector = new Vector(targetPosition.X - playerPosition.X, targetPosition.Y - playerPosition.Y);

        // calculate slowdown value
        int slowDownValue = (int)Math.Round(slowDownCalculator.CalculateSlowDown());
        Console.Error.WriteLine($"slowDownValue: {slowDownValue}");

        // calculate thrust
        int thrust = 100 - slowDownValue;

        if (thrust < 30)
            thrust = 30;
        else if (thrust > 100)
            thrust = 100;

        string sThrust = thrust.ToString();

        double moveAngle = angleCalculator.CalculateAngle(gamestateCalculator.GameStates.First().PlayerVector,
            targetVector);

        // if we pass the boostCP -> BOOOOOST...
        if (checkpointMemory.AllCheckPointsKnown &&
            checkpointMemory.GetCheckpointAtPosition(targetPosition)?.Id ==
            boostUseCalculator.GetBoostTargetCheckpoint().Id &&
            slowDownValue < 5 &&
            distanceToNextCheckpoint > 4000 &&
            moveAngle < 18 &&
            !boost)
        {
            sThrust = "BOOST";
            boost = true;
        }

        return sThrust;
    }
}

public interface IAngleCalculator
{
    double CalculateAngle(Vector vec1, Vector vc2);
}

public class AngleCalculator : IAngleCalculator
{
    #region Implementation of IAngleCalculator

    public double CalculateAngle(Vector vec1, Vector vec2)
    {
        double d = (vec1.X * vec2.X + vec1.Y * vec2.Y) /
                   Math.Pow(
                       (Math.Pow(vec1.X, 2) + Math.Pow(vec1.Y, 2)) *
                       (Math.Pow(vec2.X, 2) + Math.Pow(vec2.Y, 2)), 0.5);

        double acos = Math.Acos(d);

        double angle = acos * (180.0 / Math.PI);

        return angle;
    }

    #endregion
}

public interface ISlowDownCalculator
{
    double CalculateSlowDown();
}

public class LinearAngleSlowDownCalculator : ISlowDownCalculator
{
    private readonly ISlowDownCalculator slowDownCalculator;
    private readonly IInputContainer inputContainer;

    public LinearAngleSlowDownCalculator(ISlowDownCalculator slowDownCalculator, IInputContainer inputContainer)
    {
        this.slowDownCalculator = slowDownCalculator;
        this.inputContainer = inputContainer;
    }

    #region Implementation of ISlowDownCalculator

    public double CalculateSlowDown()
    {
        double angleSlow = 0.75 * Math.Abs(inputContainer.AngleToNextCheckPoint) - 8;
        //Console.Error.WriteLine($"angle: {angle}, angleSlow: {angleSlow}");
        angleSlow = angleSlow < 0 ? 0 : angleSlow;

        if (slowDownCalculator != null)
            angleSlow += slowDownCalculator.CalculateSlowDown();

        return angleSlow;
    }

    #endregion
}

public class LinearMovementAngleSlowDownCalculator : ISlowDownCalculator
{
    private readonly ISlowDownCalculator slowDownCalculator;
    private readonly IGamestateCalculator gamestateCalculator;
    private readonly IInputContainer inputContainer;
    private readonly IAngleCalculator angleCalculator;
    private readonly ICheckpointMemory checkpointMemory;

    public LinearMovementAngleSlowDownCalculator(ISlowDownCalculator slowDownCalculator,
        IGamestateCalculator gamestateCalculator, IInputContainer inputContainer, IAngleCalculator angleCalculator,
        ICheckpointMemory checkpointMemory)
    {
        this.slowDownCalculator = slowDownCalculator;
        this.gamestateCalculator = gamestateCalculator;
        this.inputContainer = inputContainer;
        this.angleCalculator = angleCalculator;
        this.checkpointMemory = checkpointMemory;
    }

    #region Implementation of ISlowDownCalculator

    public double CalculateSlowDown()
    {
        double angleSlow = 0;

        // slowdown value based on angle to next checkpoint and proximity
        if (inputContainer.DistanceToNextCheckPoint < 2000 && checkpointMemory.AllCheckPointsKnown)
        {
            // calculate slow down value for current vector angle to target
            GameState currentGamestate = gamestateCalculator.GameStates.First();
            double movementAngle = angleCalculator.CalculateAngle(currentGamestate.PlayerVector,
                new Vector(checkpointMemory.NextCheckpoint.Position.X - currentGamestate.PlayerPosition.X,
                    checkpointMemory.NextCheckpoint.Position.Y - currentGamestate.PlayerPosition.Y));

            angleSlow = 0.75 * Math.Abs(movementAngle) - 8;
            //Console.Error.WriteLine($"movementAngle: {movementAngle}, angleSlow: {angleSlow}");
            angleSlow = angleSlow < 0 ? 0 : angleSlow;
        }

        if (slowDownCalculator != null)
            angleSlow += slowDownCalculator.CalculateSlowDown();

        return angleSlow;
    }

    #endregion
}

public class SimpleAngleSlowDownCalculator : ISlowDownCalculator
{
    private readonly ISlowDownCalculator slowDownCalculator;
    private readonly IInputContainer inputContainer;

    public SimpleAngleSlowDownCalculator(ISlowDownCalculator slowDownCalculator, IInputContainer inputContainer)
    {
        this.slowDownCalculator = slowDownCalculator;
        this.inputContainer = inputContainer;
    }

    #region Implementation of ISlowDownCalculator

    public double CalculateSlowDown()
    {
        double angleSlow = 0;
        if (inputContainer.AngleToNextCheckPoint > 10)
            angleSlow = (inputContainer.AngleToNextCheckPoint - 10d) * 10 / 15;
        if (slowDownCalculator != null)
            angleSlow += slowDownCalculator.CalculateSlowDown();
        return angleSlow;
    }

    #endregion
}

public class SimpleDistanceSlowDownCalculator : ISlowDownCalculator
{
    private readonly ISlowDownCalculator slowDownCalculator;
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IInputContainer inputContainer;

    public SimpleDistanceSlowDownCalculator(ISlowDownCalculator slowDownCalculator, ICheckpointMemory checkpointMemory,
        IInputContainer inputContainer)
    {
        this.slowDownCalculator = slowDownCalculator;
        this.checkpointMemory = checkpointMemory;
        this.inputContainer = inputContainer;
    }

    #region Implementation of ISlowDownCalculator

    public double CalculateSlowDown()
    {
        double distSlow = 0;

        if (!checkpointMemory.AllCheckPointsKnown &&
            inputContainer.DistanceToNextCheckPoint < 2000)
            distSlow = (2000d - inputContainer.DistanceToNextCheckPoint) / 20;

        if (slowDownCalculator != null)
            distSlow += slowDownCalculator.CalculateSlowDown();

        return distSlow;
    }

    #endregion
}

public class HitPredictionSlowDownCalculator : ISlowDownCalculator
{
    private readonly ISlowDownCalculator slowDownCalculator;
    private readonly IGamestateCalculator gamestateCalculator;
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IInputContainer inputContainer;

    public HitPredictionSlowDownCalculator(ISlowDownCalculator slowDownCalculator,
        IGamestateCalculator gamestateCalculator, ICheckpointMemory checkpointMemory, IInputContainer inputContainer)
    {
        this.slowDownCalculator = slowDownCalculator;
        this.gamestateCalculator = gamestateCalculator;
        this.checkpointMemory = checkpointMemory;
        this.inputContainer = inputContainer;
    }

    private double HitsCheckpoint(Point start, Point end, Point cpCenter, int radius)
    {
        double a = Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2);

        double b = 2 * (end.X - start.X) * (start.X - cpCenter.X) + 2 * (end.Y - start.Y) * (start.Y - cpCenter.Y);

        double c = Math.Pow(start.X - cpCenter.X, 2) + Math.Pow(start.Y - cpCenter.Y, 2) - Math.Pow(radius, 2);

        double t = (2 * c) / (-b + Math.Sqrt(Math.Pow(b, 2) - 4 * a * c));

        return t;

        //int tx = (int)Math.Round((end.X - start.X) * t + start.X);

        //int ty = (int)Math.Round((end.Y - start.Y) * t + start.Y);
    }

    #region Implementation of ISlowDownCalculator

    public double CalculateSlowDown()
    {
        double slowDown = 0;

        int hitsCheckPoint = -1;
        Point oldPosition = new Point(Int32.MinValue, Int32.MinValue);

        foreach (GameState state in gamestateCalculator.GameStates)
        {
            double chechkpointHitRoot = HitsCheckpoint(oldPosition, state.PlayerPosition,
                checkpointMemory.CurrentCheckpoint.Position, 590);

            if (oldPosition.X != Int32.MinValue &&
                oldPosition.Y != Int32.MinValue &&
                chechkpointHitRoot > 0 &&
                chechkpointHitRoot < 1)
            {
                hitsCheckPoint = state.TickOffset;
                break;
            }

            oldPosition = state.PlayerPosition;
        }

        Console.Error.WriteLine($"hitsCheckPoint: {hitsCheckPoint}");
        Console.Error.WriteLine($"AngleToNextCheckPoint: {inputContainer.AngleToNextCheckPoint}");
        if (inputContainer.DistanceToNextCheckPoint < 4000 &&
            hitsCheckPoint == -1 &&
            inputContainer.AngleToNextCheckPoint > 18)
        {
            slowDown =
                (int)Math.Round(-0.0264705882352941 * inputContainer.DistanceToNextCheckPoint + 105.8823529411765);
            Console.Error.WriteLine($"hitPredictionSlowDown: {slowDown}");
        }

        if (slowDownCalculator != null)
            slowDown += slowDownCalculator.CalculateSlowDown();

        return slowDown;
    }

    #endregion
}

public class GameState
{
    public int TickOffset { get; private set; }

    public Point PlayerPosition { get; private set; }

    public Vector PlayerVector { get; private set; }

    public GameState(int tickOffset, Point playerPosition, Vector playerVector)
    {
        TickOffset = tickOffset;
        PlayerPosition = playerPosition;
        PlayerVector = playerVector;
    }
}

public interface IGamestateCalculator
{
    IEnumerable<GameState> GameStates { get; }

    void Recalculate();
}

public class GamestateCalculator : IGamestateCalculator
{
    private readonly IInputContainer inputContainer;

    private Point lastPosition;

    public IEnumerable<GameState> GameStates { get; private set; }

    public GamestateCalculator(IInputContainer inputContainer)
    {
        this.inputContainer = inputContainer;
        GameStates = new List<GameState>();
        lastPosition = new Point(-1, -1);
    }

    public void Recalculate()
    {
        Vector currentVector = new Vector(inputContainer.PlayerPosition.X - lastPosition.X,
            inputContainer.PlayerPosition.Y - lastPosition.Y);

        List<GameState> gameStates = new List<GameState>
        {
            new GameState(0, new Point(inputContainer.PlayerPosition.X, inputContainer.PlayerPosition.Y), currentVector)
        };

        for (int i = 0; i < 6; i++)
        {
            GameState gameState = new GameState(i + 1,
                new Point(gameStates[i].PlayerPosition.X + currentVector.X,
                    gameStates[i].PlayerPosition.Y + currentVector.Y), currentVector);
            gameStates.Add(gameState);

            //Console.Error.WriteLine($"gameState: {gameState.TickOffset},{gameState.PlayerPosition}");
        }

        GameStates = gameStates;

        lastPosition = inputContainer.PlayerPosition;
    }
}