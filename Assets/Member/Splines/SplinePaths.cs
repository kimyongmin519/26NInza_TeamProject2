using Reflex.Core;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    public class SplinePaths : MonoBehaviour, IInstaller
    {
        [field: SerializeField] public SplinePath[] Paths { get; private set; }

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterValue(this);
        }
    }
}
