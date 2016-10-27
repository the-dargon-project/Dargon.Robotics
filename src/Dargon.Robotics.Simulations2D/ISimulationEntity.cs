using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Dargon.Robotics.Simulations2D {
   public interface ISimulationEntity {
      void Initialize(Simulation2D simulation, World world);
      void SetLocalCenter(Vector2 vector);
      void Render(IRenderer renderer);
      void Delete();
      bool Tick(float dtSeconds);
   }
}