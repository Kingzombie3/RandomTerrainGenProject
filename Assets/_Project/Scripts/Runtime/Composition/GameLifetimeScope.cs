using _Project.Scripts.Runtime.Features.WorldGeneration;
using VContainer;
using VContainer.Unity;

namespace _Project.Scripts.Runtime.Composition
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<NoiseGenerator>(Lifetime.Singleton).As<INoiseGenerator>();
        }
     
    }
}