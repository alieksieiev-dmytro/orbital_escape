using System.Collections.Generic;
using OrbitalEscape.Orbits;
using OrbitalEscape.Utils;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public sealed class ImpulseTrapFactory : IObjectFactory<GameObject>
    {
        private readonly List<GameObject> _trapObjects;
        private readonly GameObject _trapPrefab;

        public ImpulseTrapFactory(GameObject trapPrefab)
        {
            _trapObjects = new List<GameObject>();
            _trapPrefab = trapPrefab;
        }

        public GameObject Create(params object[] args)
        {
            var root = (Transform)args[0];
            var orbitData = (Orbit)args[1];
            var trapData = (ArcObject)args[2];

            var trapObject = Object.Instantiate(_trapPrefab, Constants.Center3D, Quaternion.identity, root);
            trapObject.name = $"impulse_trap_{trapData.Type}";

            trapObject.transform.localScale =
                new Vector3(trapData.Arc.Length() / root.localScale.x,
                    ((ImpulseTrap)trapData.Object).Height / root.localScale.y,
                    1f);

            float radius = trapData.Type switch
            {
                ArcType.Inner => orbitData.Radius - 0.1f,
                ArcType.Outer => orbitData.Radius,
                _ => 0f
            };

            Vector2 position = OrbitMath.GetPointOnCircle(
                Constants.Center,
                radius,
                trapData.Arc.MidAngleDegree()) / root.localScale.x;

            Vector2 dir = trapData.Type switch
            {
                ArcType.Inner => Constants.Center - position,
                ArcType.Outer => position - Constants.Center,
                _ => Vector2.zero
            };

            float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            trapObject.transform.rotation = Quaternion.Euler(0f, 0f, rotation + 90.0f);
            trapObject.transform.localPosition = new Vector3(position.x, position.y, 0f);
            _trapObjects.Add(trapObject);

            return trapObject;
        }

        public GameObject Get(int index) => _trapObjects[index];

        public void Clear()
        {
            foreach (var trapObject in _trapObjects)
            {
                Object.Destroy(trapObject);
            }

            _trapObjects.Clear();
        }
    }
}
