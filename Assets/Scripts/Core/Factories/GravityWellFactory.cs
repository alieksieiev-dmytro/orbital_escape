using System.Collections.Generic;
using OrbitalEscape.Orbits;
using OrbitalEscape.Utils;
using OrbitalEscape.Visuals;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public sealed class GravityWellFactory : IObjectFactory<GameObject>
    {
        private readonly List<GameObject> _gravityWellObjects;
        private readonly GameObject _gravityWellPrefab;

        public GravityWellFactory(GameObject gravityWellPrefab)
        {
            _gravityWellObjects = new List<GameObject>();
            _gravityWellPrefab = gravityWellPrefab;
        }

        public GameObject Create(params object[] args)
        {
            Transform root = (Transform)args[0];
            Orbit sourceOrbit = (Orbit)args[1];
            ArcObject gravityWellData = (ArcObject)args[2];
            Orbit targetOrbit = (Orbit)args[3];

            GravityWell gravityWell = (GravityWell)gravityWellData.Object;
            GameObject gravityWellObject = Object.Instantiate(
                _gravityWellPrefab,
                Constants.Center3D,
                Quaternion.identity,
                root);
            gravityWellObject.name = $"gravity_well_{gravityWellData.Type}";

            float innerRadius = Mathf.Min(sourceOrbit.Radius, targetOrbit.Radius);
            float outerRadius = Mathf.Max(sourceOrbit.Radius, targetOrbit.Radius);
            float arcRadius = (innerRadius + outerRadius) * 0.5f;
            float arcThickness = outerRadius - innerRadius;
            float coverDiameter = outerRadius * 2.0f;
            Vector2 localPosition = OrbitMath.WorldToLocalPoint(root, Constants.Center);

            gravityWellObject.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0.0f);
            gravityWellObject.transform.localRotation = Quaternion.identity;
            gravityWellObject.transform.localScale = new Vector3(
                ToLocalScale(coverDiameter, root.lossyScale.x),
                ToLocalScale(coverDiameter, root.lossyScale.y),
                1.0f);

            if (gravityWellObject.GetComponentInChildren<GravityWellArcVisual>(true) is { } visual)
            {
                visual.Configure(
                    Constants.Center,
                    arcRadius,
                    arcThickness,
                    Mathf.Deg2Rad * OrbitMath.NormalizeDegrees360(gravityWellData.Arc.StartAngleDegree),
                    Mathf.Deg2Rad * OrbitMath.DeltaDegrees(
                        gravityWellData.Arc.StartAngleDegree,
                        gravityWellData.Arc.EndAngleDegree),
                    gravityWell.VisualAlpha,
                    sourceOrbit.Radius > targetOrbit.Radius);
            }

            _gravityWellObjects.Add(gravityWellObject);
            return gravityWellObject;
        }

        public GameObject Get(int index) => _gravityWellObjects[index];

        public void Clear()
        {
            foreach (GameObject gravityWellObject in _gravityWellObjects)
            {
                Object.Destroy(gravityWellObject);
            }

            _gravityWellObjects.Clear();
        }

        private static float ToLocalScale(float worldSize, float parentScale)
        {
            return Mathf.Approximately(parentScale, 0.0f)
                ? worldSize
                : worldSize / parentScale;
        }
    }
}
