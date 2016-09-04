﻿using System;
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
        ITargetFinding targetFinding = new TargetFinder(checkpointMemory, inputContainer, speedCalculator,
            angleCalculator);
        IThrustCalculator thrustCalculator = new ThrustCalculator(checkpointMemory, boostUseCalculator, angleCalculator);

        new Player().Start(inputContainer, initialCheckpointGuesser, checkpointMemory, boostUseCalculator, targetFinding,
            thrustCalculator);
    }

    public void Start(IInputContainer inputContainer, IInitialCheckpointGuesser initialCheckpointGuesser,
        ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator,
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

            Point target = targetFinding.GetTarget(inputContainer.PlayerPosition, currentCp, nextCp, cpAfterNextCp);

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

public class Vector
{
    public int X { get; }
    public int Y { get; }
    //public int Length { get; set; }

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
        Console.Error.WriteLine(p);
        knownCheckpoints.ForEach(x => Console.Error.WriteLine(x.Id + " " + x.Position));
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
            Console.Error.WriteLine(cp.Id + " " + cp.DistToNext);
        }

        // find Checkpoint with longest distance to next Checkpoint
        int boostCpId = checkpointMemory.KnownCheckpoints.ToList().OrderByDescending(item => item.DistToNext).First().Id;
        Console.Error.WriteLine("boostCpId: " + boostCpId);

        // find and return next Checkpoint (the Checkpoint to boost to)
        Checkpoint boostCheckpoint = checkpointMemory.KnownCheckpoints.ToList().Find(x => x.Id == boostCpId + 1);
        Console.Error.WriteLine("boostCheckpoint.Id: " + boostCheckpoint?.Id);
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
    Point GetTarget(Point currentPosition, Checkpoint currentCp, Checkpoint nextCp,
        Checkpoint checkpointAftertNextCheckpoint);
}

public class TargetFinder : ITargetFinding
{
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IInputContainer inputContainer;
    private readonly ISpeedCalculator speed;
    private readonly IAngleCalculator angleCalculator;

    public TargetFinder(ICheckpointMemory checkpointMemory, IInputContainer inputContainer, ISpeedCalculator speed,
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
                speed.GetSpeed(currentPosition) > 500)
                target = GetDesiredPoint(currentCp.Position, nextCp.Position, checkpointAftertNextCheckpoint.Position);
            else
                target = GetDesiredPoint(currentPosition, currentCp.Position, nextCp.Position);
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

        // Calculate directional vector to halfed angle
        double cos = Math.Cos(angleInRad);
        double sin = Math.Sin(angleInRad);

        // calculate factor to 400pt away from center
        double fac1 = 400 / cos;
        double fac2 = 400 / sin;
        double factor = Math.Abs(fac1) <= Math.Abs(fac2) ? fac1 : fac2;

        // calculate vector from directional vector and factor (width)
        double x = cos * factor;
        double y = sin * factor;

        // Check x and y of vector for positive/negative values and adjust accordingly
        if ((vecFromNextToFollowingCp.X > 0 && x < 0) ||
            (vecFromNextToFollowingCp.X < 0 && x > 0))
            x *= -1;
        if ((vecFromNextToFollowingCp.Y > 0 && y < 0) ||
            (vecFromNextToFollowingCp.Y < 0 && y > 0))
            y *= -1;

        // create vector to desired point from currentCp
        int roundX = (int)Math.Round(x);
        int roundY = (int)Math.Round(y);
        Console.Error.WriteLine("roundX: "+roundX);
        Console.Error.WriteLine("roundY: " + roundY);
        Vector v = new Vector(roundX, roundY);
        return new Point(p2.X + v.X, p2.Y + v.Y);
    }
}

public interface IThrustCalculator
{
    string GetThrust(int distanceToNextCheckpoint, int angleToNextCheckpoint, Point playerPosition,
        Point targetPosition);
}

public class ThrustCalculator : IThrustCalculator
{
    private readonly ICheckpointMemory checkpointMemory;
    private readonly IBoostUseCalculator boostUseCalculator;
    private readonly IAngleCalculator angleCalculator;
    private Point? lastPosition;
    private bool boost;

    public ThrustCalculator(ICheckpointMemory checkpointMemory, IBoostUseCalculator boostUseCalculator,
        IAngleCalculator angleCalculator)
    {
        this.checkpointMemory = checkpointMemory;
        this.boostUseCalculator = boostUseCalculator;
        this.angleCalculator = angleCalculator;
        boost = false;
    }

    public string GetThrust(int distanceToNextCheckpoint, int angleToNextCheckpoint, Point playerPosition,
        Point targetPosition)
    {
        // calculate slow down value for distance to next target
        int distSlow = 0;
        //if (distanceToNextCheckpoint < 2000)
        //    distSlow = (2000 - distanceToNextCheckpoint) / 20;

        // calculate slow down value for angle to next checkpoint
        int angleSlow = 0;
        //if (Math.Abs(angleToNextCheckpoint) > 10)
        //    angleSlow = (Math.Abs(angleToNextCheckpoint) - 10) /** * 10 / 10 **/;

        if (lastPosition != null)
        {
            // calculate slow down value for current vector angle to next checkpoint
            Vector currentVector = new Vector(playerPosition.X - lastPosition.Value.X,
                playerPosition.Y - lastPosition.Value.Y);
            Vector targetVector = new Vector(targetPosition.X - playerPosition.X, targetPosition.Y - playerPosition.Y);

            double currentVectorAngle = angleCalculator.CalculateAngle(currentVector, targetVector);
            Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

            if (currentVectorAngle > 10)
                angleSlow += (int)Math.Round(currentVectorAngle - 10) / 2;
            Console.Error.WriteLine("angleSlow: " + angleSlow);
        }

        // calculate thrust
        int thrust = 100 - distSlow - angleSlow;
        if (thrust < 5)
            thrust = 5;

        string sThrust = thrust.ToString();

        // if we pass the boostCP -> BOOOOOST...
        if (checkpointMemory.AllCheckPointsKnown &&
            checkpointMemory.GetCheckpointAtPosition(targetPosition)?.Id ==
            boostUseCalculator.GetBoostTargetCheckpoint().Id &&
            angleSlow < 5 &&
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