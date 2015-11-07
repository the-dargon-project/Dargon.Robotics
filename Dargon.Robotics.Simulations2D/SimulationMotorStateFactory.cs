using Microsoft.Xna.Framework;

namespace Dargon.Robotics.Simulations2D {
   public static class SimulationMotorStateFactory {
      /// <summary>
      /// Given robot :[^]:, tl/br vect is \, tr/bl vect is /
      /// </summary>
      /// <param name="robotWidth">Width of robot</param>
      /// <param name="robotHeight">Height of robot (well, y-axis 'height')</param>
      /// <param name="wheelForceAngle">magnitude of mecanum wheels' force vectors' angles (relative to ^ direction) in radians</param>
      /// <param name="wheelForceAmplitude">Maximum effective force applied by wheel</param>
      /// <returns></returns>
      public static SimulationMotorState[] SkidDrive(float robotWidth, float robotHeight, float wheelForceAmplitude) {
         return MecanumDrive(robotWidth, robotHeight, 0, wheelForceAmplitude);
      }

      /// <summary>
      /// Given robot :[^]:, tl/br vect is \, tr/bl vect is /
      /// </summary>
      /// <param name="robotWidth">Width of robot</param>
      /// <param name="robotHeight">Height of robot (well, y-axis 'height')</param>
      /// <param name="wheelForceAngle">magnitude of mecanum wheels' force vectors' angles (relative to ^ direction) in radians</param>
      /// <param name="wheelForceAmplitude">Maximum effective force applied by wheel</param>
      /// <returns></returns>
      public static SimulationMotorState[] MecanumDrive(float robotWidth, float robotHeight, float wheelForceAngle, float wheelForceAmplitude) {
         var forceVector = new Vector2(0, wheelForceAmplitude);
         var forceTopLeft = Vector2.Transform(forceVector, Matrix.CreateRotationZ(-wheelForceAngle));
         var forceTopRight = Vector2.Transform(forceVector, Matrix.CreateRotationZ(wheelForceAngle));
         var motorStates = new SimulationMotorState[4];
         float halfWidth = robotWidth / 2, halfHeight = robotHeight / 2;
         float backFrontSpacing = halfHeight / 2;
         motorStates[0] = new SimulationMotorState(new Vector2(-halfWidth, -backFrontSpacing), forceTopLeft);
         motorStates[1] = new SimulationMotorState(new Vector2(halfWidth, -backFrontSpacing), forceTopRight);
         motorStates[2] = new SimulationMotorState(new Vector2(halfWidth, backFrontSpacing), forceTopLeft);
         motorStates[3] = new SimulationMotorState(new Vector2(-halfWidth, backFrontSpacing), forceTopRight);
         return motorStates;
      }
   }
}