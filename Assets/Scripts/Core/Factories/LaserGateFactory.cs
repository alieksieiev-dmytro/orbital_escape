using System.Collections.Generic;
using OrbitalEscape.Orbits;
using OrbitalEscape.Utils;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public sealed class LaserGateFactory : IObjectFactory<GameObject>
    {
        private readonly List<GameObject> _laserGateObjects;
        private readonly GameObject _laserGatePrefab;

        public LaserGateFactory(GameObject laserGatePrefab)
        {
            _laserGateObjects = new List<GameObject>();
            _laserGatePrefab = laserGatePrefab;
        }

        public GameObject Create(params object[] args)
        {
            Transform root = (Transform)args[0];
            Orbit sourceOrbit = (Orbit)args[1];
            ArcObject laserGateData = (ArcObject)args[2];
            Orbit targetOrbit = (Orbit)args[3];

            LaserGate laserGate = (LaserGate)laserGateData.Object;
            float width = Mathf.Max(0.01f, laserGateData.Arc.Length());
            float innerMovementRadius = GetInnerMovementRadius(sourceOrbit, targetOrbit);
            float outerMovementRadius = GetOuterMovementRadius(sourceOrbit, targetOrbit);
            float height = Mathf.Max(0.01f, outerMovementRadius - innerMovementRadius);
            float midpointRadius = (innerMovementRadius + outerMovementRadius) * 0.5f;
            float angle = laserGateData.Arc.MidAngleDegree();

            GameObject laserGateObject = Object.Instantiate(
                _laserGatePrefab,
                Constants.Center3D,
                Quaternion.identity,
                root);
            laserGateObject.name = $"laser_gate_{laserGateData.Type}";

            Vector2 midpoint = OrbitMath.GetPointOnCircle(Constants.Center, midpointRadius, angle);
            Vector2 localPosition = OrbitMath.WorldToLocalPoint(root, midpoint);
            Vector2 outwardDirection = (midpoint - Constants.Center).normalized;
            float rotation = Mathf.Atan2(outwardDirection.y, outwardDirection.x) * Mathf.Rad2Deg - 90.0f;

            laserGateObject.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0.0f);
            laserGateObject.transform.rotation = Quaternion.Euler(0.0f, 0.0f, rotation);
            laserGateObject.transform.localScale = new Vector3(
                ToLocalScale(1.0f, root.lossyScale.x),
                ToLocalScale(1.0f, root.lossyScale.y),
                1.0f);

            if (laserGateObject.GetComponent<LaserGateController>() is { } controller)
            {
                controller.Fit(width, height);
                controller.Configure(laserGate);
            }

            _laserGateObjects.Add(laserGateObject);
            return laserGateObject;
        }

        public GameObject Get(int index) => _laserGateObjects[index];

        public void Clear()
        {
            foreach (GameObject laserGateObject in _laserGateObjects)
            {
                Object.Destroy(laserGateObject);
            }

            _laserGateObjects.Clear();
        }

        private static float GetInnerMovementRadius(Orbit first, Orbit second)
        {
            float radius = Mathf.Min(first.Radius, second.Radius);
            return radius + Constants.CHARACTER_OFFSET_FROM_ORBIT;
        }

        private static float GetOuterMovementRadius(Orbit first, Orbit second)
        {
            float radius = Mathf.Max(first.Radius, second.Radius);
            return radius - Constants.CHARACTER_OFFSET_FROM_ORBIT;
        }

        private static float ToLocalScale(float worldSize, float parentScale)
        {
            return Mathf.Approximately(parentScale, 0.0f)
                ? worldSize
                : worldSize / parentScale;
        }
    }
}
