using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

internal class Player
{
    private static void Main()
    {
        IInputManager inputManager = new InputManager(new ConsoleInputGetter());

        bool boost = false;
        List<Checkpoint> checkpoints = new List<Checkpoint>();
        int checkpointCounter = 1;
        bool allCheckpointsKnown = false;
        int boostCpId = -1;
        int speed = -1;
        Point lastPosition = new Point(-1, -1);

        while (true)
        {
            #region inputs

            inputManager.Update();

            #endregion

            #region Checkpoint calculations

            // Set first Checkpoint early (guessing)
            if (checkpoints.Count == 0)
            {
                // We guess that the first/final Checkpoint is in between our pod and the opponent
                int diffX = inputManager.OpponentPosition.X - inputManager.PlayerPosition.X;
                int diffY = inputManager.OpponentPosition.Y - inputManager.PlayerPosition.Y;

                var initialCp = new Checkpoint(0, inputManager.PlayerPosition.X + diffX,
                    inputManager.PlayerPosition.Y + diffY);
                checkpoints.Add(initialCp);
            }

            // Create a new Checkpoint with current target if we don't know all Checkpoints yet
            Checkpoint newCp = null;
            if (!allCheckpointsKnown)
            {
                newCp = new Checkpoint(checkpointCounter, inputManager.NextCheckpointLocation.X,
                    inputManager.NextCheckpointLocation.Y);
            }

            // Try to get the current target CP. If its null, then we add the newCP and set it as current
            // we use a threshold of 600 because we guessed the first Checkpoint
            var currentCp =
                checkpoints.Find(
                    cp =>
                        (cp.X >= inputManager.NextCheckpointLocation.X - 600 &&
                         cp.X <= inputManager.NextCheckpointLocation.X + 600) &&
                        (cp.Y >= inputManager.NextCheckpointLocation.Y - 600 &&
                         cp.Y <= inputManager.NextCheckpointLocation.Y + 600));
            if (currentCp == null)
            {
                checkpoints.Add(newCp);
                checkpointCounter++;
                currentCp = newCp;
            }

            // if we target the first Checkpoint we can safely say, that we know all Checkpoints
            if (currentCp.Id == 0 &&
                !allCheckpointsKnown)
            {
                // update the first Checkpoint with exact values
                checkpoints[0] = new Checkpoint(0, inputManager.NextCheckpointLocation.X,
                    inputManager.NextCheckpointLocation.Y);

                allCheckpointsKnown = true;

                // calculate the checkpoint on which to use boost (checkpoint with greatest dist to next checkpoint)
                foreach (var cp in checkpoints)
                {
                    var cpNext = checkpoints.Find(ncp => ncp.Id == cp.Id + 1);
                    if (cpNext == null)
                        cpNext = checkpoints[0];

                    int distX = cpNext.X - cp.X;
                    int distY = cpNext.Y - cp.Y;

                    cp.DistToNext = Math.Abs(distX) + Math.Abs(distY);
                }

                boostCpId = checkpoints.OrderByDescending(item => item.DistToNext).First().Id;
            }

            #endregion

            #region speed calculations

            if (lastPosition.X != -1 &&
                lastPosition.Y != -1)
            {
                speed = Math.Abs(inputManager.PlayerPosition.X - lastPosition.X) +
                        Math.Abs(inputManager.PlayerPosition.Y - lastPosition.Y);
            }

            #endregion

            #region target finding

            int nextTargetX = inputManager.NextCheckpointLocation.X;
            int nextTargetY = inputManager.NextCheckpointLocation.Y;

            if (allCheckpointsKnown &&
                inputManager.DistanceToNextCheckPoint < 1500 &&
                speed > 500)
            {
                Console.Error.WriteLine("currentCP: " + currentCp.Id);
                var nextTargetCp = checkpoints.Find(cp => cp.Id == currentCp.Id + 1);
                if (nextTargetCp == null)
                    nextTargetCp = checkpoints[0];
                Console.Error.WriteLine("nextTargetCP: " + nextTargetCp.Id);

                nextTargetX = nextTargetCp.X;
                nextTargetY = nextTargetCp.Y;
            }

            #endregion

            #region thrust calculations

            // calculate slow down value for distance to next target
            int nextTargetDist = Math.Abs(inputManager.NextCheckpointLocation.X - inputManager.PlayerPosition.X) +
                                 Math.Abs(inputManager.NextCheckpointLocation.Y - inputManager.PlayerPosition.Y);
            int distSlow = 0;
            if (nextTargetDist < 2000)
                distSlow = (2000 - nextTargetDist) / 20;

            // calculate slow down value for angle to next checkpoint
            int angleSlow = 0;
            if (Math.Abs(inputManager.AngleToNextCheckPoint) > 10)
                angleSlow = (Math.Abs(inputManager.AngleToNextCheckPoint) - 10) /** * 10 / 10 **/;

            // calculate slow down value for current vector angle to next checkpoint
            int currentVectorX = inputManager.NextCheckpointLocation.X - lastPosition.X;
            int currentVectorY = inputManager.NextCheckpointLocation.Y - lastPosition.Y;
            int nextCpVectorX = inputManager.NextCheckpointLocation.X - inputManager.NextCheckpointLocation.X;
            int nextCpVectorY = inputManager.NextCheckpointLocation.Y - inputManager.NextCheckpointLocation.Y;

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
            var boostCp = checkpoints.Find(cp => cp.Id == currentCp.Id - 1);
            if (boostCp == null)
                boostCp = checkpoints[checkpoints.Count - 1];
            if (angleSlow == 0 &&
                boostCpId == boostCp.Id &&
                inputManager.DistanceToNextCheckPoint > 4000 &&
                !boost)
            {
                sThrust = "BOOST";
                boost = true;
            }

            #endregion

            #region status messages

            if (false)
            {
                foreach (var cp in checkpoints)
                    Console.Error.WriteLine(cp.Id + " " + cp.X + " " + cp.Y + " " + cp.DistToNext);

                Console.Error.WriteLine("cp count: " + checkpoints.Count);

                Console.Error.WriteLine("allCheckpointsKnown: " + allCheckpointsKnown);

                Console.Error.WriteLine("nextCheckpointDist: " + inputManager.DistanceToNextCheckPoint);

                Console.Error.WriteLine("nextCheckpointAngle: " + inputManager.AngleToNextCheckPoint);

                Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

                Console.Error.WriteLine("currentVectorX: " + currentVectorX);
                Console.Error.WriteLine("currentVectorY: " + currentVectorY);

                Console.Error.WriteLine("nextCpVectorX: " + nextCpVectorX);
                Console.Error.WriteLine("nextCpVectorY: " + nextCpVectorY);

                Console.Error.WriteLine("lastPosition: " + lastPosition);
                Console.Error.WriteLine("currentPosition: " + inputManager.PlayerPosition);

                Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);
                Console.Error.WriteLine("currentVectorAngle: " + currentVectorAngle);

                Console.Error.WriteLine("distSlow: " + distSlow);

                Console.Error.WriteLine("angleSlow: " + angleSlow);

                Console.Error.WriteLine("boostCpId: " + boostCpId);

                Console.Error.WriteLine("thrust: " + thrust);
            }

            #endregion

            lastPosition = inputManager.PlayerPosition;

            Console.WriteLine(nextTargetX + " " + nextTargetY + " " + sThrust);
        }
    }
}

internal class Checkpoint
{
    internal int Id { get; private set; }
    internal int X { get; private set; }
    internal int Y { get; private set; }
    internal int DistToNext { get; set; }

    internal Checkpoint(int id, int x, int y)
    {
        Id = id;
        X = x;
        Y = y;
    }
}

public interface IInputManager
{
    int AngleToNextCheckPoint { get; }
    int DistanceToNextCheckPoint { get; }
    Point NextCheckpointLocation { get; }
    Point OpponentPosition { get; }
    Point PlayerPosition { get; }

    void Update();
}

public class InputManager : IInputManager
{
    private readonly IInputGetter inputGetter;
    public Point PlayerPosition { get; private set; }

    public Point NextCheckpointLocation { get; private set; }

    public int DistanceToNextCheckPoint { get; private set; }

    public int AngleToNextCheckPoint { get; private set; }

    public Point OpponentPosition { get; private set; }

    public InputManager(IInputGetter inputGetter)
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