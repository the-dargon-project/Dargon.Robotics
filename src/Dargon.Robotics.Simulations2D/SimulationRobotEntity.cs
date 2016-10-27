using System;
using Dargon.Commons;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Dargon.Robotics.Simulations2D;

namespace Dargon.Robotics.Simulations2D {
   public class SimulationRobotEntity : ISimulationEntity {
      private readonly SimulationConstants constants;
      private readonly SimulationRobotState robotState;
      private readonly Vector2 centerOfMass;
      private readonly Vector2 initialPosition;
      private readonly float nonforwardMotionSuppressionFactor;
      private Body robotBody;
      private Simulation2D simulation;

      public SimulationRobotEntity(SimulationConstants constants, SimulationRobotState robotState, Vector2 centerOfMass = default(Vector2), Vector2 initialPosition = default(Vector2), float nonforwardMotionSuppressionFactor = 0.0f) {
         this.constants = constants;
         this.robotState = robotState;
         this.centerOfMass = centerOfMass;
         this.initialPosition = initialPosition;
         this.nonforwardMotionSuppressionFactor = nonforwardMotionSuppressionFactor;
      }

      public void Initialize(Simulation2D simulation, World world) {
         robotBody = BodyFactory.CreateRectangle(world, robotState.Width, robotState.Height, robotState.Density);
         robotBody.BodyType = BodyType.Dynamic;
         robotBody.Position = initialPosition;
         robotBody.LocalCenter = centerOfMass;
         //         robotBody.Rotation = (float)Math.PI;
         robotBody.AngularDamping = constants.AngularDamping;
         robotBody.LinearDamping = constants.LinearDamping;
         robotBody.UserData = "robot";

         robotBody.OnCollision += (fixture1, fixture2, contact) => {
             Console.WriteLine(fixture1.Body.UserData);
             Console.WriteLine(fixture2.Body.UserData);
             Console.WriteLine();
             if ("ball".Equals(fixture1.Body.UserData)) {
                simulation.DeleteEntity(fixture1.Body);
               
                return false;
             } else if ("ball".Equals(fixture2.Body.UserData)) {
                simulation.DeleteEntity(fixture2.Body);

                return false;
             }
             return true;
         };
      }

      public void SetLocalCenter(Vector2 vector) {
         robotBody.LocalCenter = vector;
      }

      public void Render(IRenderer renderer) {
         DrawRobotBody(renderer);
         robotState.MotorStates.ForEach(x => DrawMotorBody(renderer, x));
      }

      private void DrawRobotBody(IRenderer renderer) {
         renderer.DrawCenteredRectangleWorld(new Vector2(10, 10), Vector2.One / 4, 0, Color.DarkGray);

         renderer.DrawCenteredRectangleWorld(
            robotBody.Position,
            new Vector2(robotState.Width, robotState.Height),
            robotBody.Rotation,
            Color.Red);
         
         // Draw up vector
         renderer.DrawLineSegmentWorld(
            robotBody.GetWorldPoint(robotBody.LocalCenter),
            robotBody.GetWorldPoint(Vector2.UnitY),
            Color.Lime);

         // Draw friction vector
//         renderer.DrawForceVectorWorld(
//            robotBody.Position,
//            ComputeRobotFrictionForceWorld(),
//            Color.Lime);

      }

      private void DrawMotorBody(IRenderer renderer, SimulationMotorState motorState) {
         var position = robotBody.GetWorldPoint(motorState.RelativePosition);
         var extents = new Vector2(robotState.Width / 10, robotState.Height / 10);
         renderer.DrawCenteredRectangleWorld(
            position,
            extents,
            robotBody.Rotation,
            Color.Lime);
         var forceVectorWorld = Vector2.Transform(motorState.CurrentForceVector, Matrix.CreateRotationZ(robotBody.Rotation));
         renderer.DrawForceVectorWorld(position, forceVectorWorld, Color.Cyan);
      }

      private void ApplyForces() {
         var forward = Vector2.Transform(Vector2.UnitY, Matrix.CreateRotationZ(robotBody.Rotation));
         if (robotBody.LinearVelocity.Length() > 0) {
            var linearVelocityNormalized = robotBody.LinearVelocity;
            linearVelocityNormalized.Normalize();
            var forwardOnlyMotion = linearVelocityNormalized * Math.Abs(Vector2.Dot(robotBody.LinearVelocity, forward));
            var forwardOnlyMotionRatio = nonforwardMotionSuppressionFactor;
            var nonforwardMotionRatio = 1.0 - forwardOnlyMotionRatio;
            robotBody.LinearVelocity = robotBody.LinearVelocity * (float)nonforwardMotionRatio + forwardOnlyMotion * (float)forwardOnlyMotionRatio;
         }

         var motors = robotState.MotorStates;
         motors.ForEach(ApplyMotorForces);
//         motors.ForEach(ApplyWheelDragFrictionForces);
      }

      private void ApplyMotorForces(SimulationMotorState motorState) {
         var forceVectorWorld = Vector2.Transform(motorState.CurrentForceVector, Matrix.CreateRotationZ(robotBody.Rotation));
//         Console.WriteLine(forceVectorWorld);
         robotBody.ApplyForce(
            forceVectorWorld,
            robotBody.GetWorldPoint(motorState.RelativePosition)
            );
         //         renderer.DrawForceVectorWorld(position, forceVectorWorld, Color.Cyan);
      }

//      private void ApplyWheelDragFrictionForces(SimulationMotorState motorState) {
//         var forceVectorWorld = Vector2.Transform(
//            motorState.CurrentForceVector, 
//            Matrix.CreateRotationZ(robotBody.Rotation));
//      }

      public bool Tick(float dtSeconds) {
         if (robotBody.IsDisposed)
            return false;
         
         robotState.WheelShaftEncoderStates.ForEach(x => UpdateWheelShaftEncoder(dtSeconds, x));

         var yawGyroscope = robotState.YawGyroscopeState;
         yawGyroscope.Angle = robotBody.Rotation;
         yawGyroscope.AngularVelocity = robotBody.AngularVelocity;

         var position = robotBody.Position;
         var velocity = robotBody.LinearVelocity;

         ApplyForces();

         return true;
      }

      private void UpdateWheelShaftEncoder(float dtSeconds, SimulationWheelShaftEncoderState wheelShaftEncoderState) {
         var motor = wheelShaftEncoderState.Motor;
         var currentWorldPosition = robotBody.GetWorldPoint(motor.RelativePosition);
         if (!wheelShaftEncoderState.HasBeenUpdated) {
            wheelShaftEncoderState.HasBeenUpdated = true;
         } else {
            var deltaPositionAbsolute = currentWorldPosition - wheelShaftEncoderState.LastWorldPosition;
            var deltaPositionRelative = Vector2.Transform(deltaPositionAbsolute, Matrix.CreateRotationZ(-robotBody.Rotation));
            var countedDirection = motor.MaxForceVector;
            countedDirection.Normalize();
            var deltaPosition = Vector2.Dot(deltaPositionRelative, countedDirection);
//            var deltaPosition = deltaPositionRelative.Length();
            var velocity = deltaPosition / dtSeconds;
            // arc length = theta * r => theta = arc length / r
            wheelShaftEncoderState.Angle += deltaPosition / wheelShaftEncoderState.WheelRadius;
            // omega = delta arc length / r
            wheelShaftEncoderState.AngularVelocity = velocity / wheelShaftEncoderState.WheelRadius;
         }
         wheelShaftEncoderState.LastWorldPosition = currentWorldPosition;
      }
   }
}