using UnityEngine;

namespace OrbitalEscape
{
    public static class Constants
    {
        public static readonly Vector2 Center = new Vector2(0, 0);
        public static readonly Vector3 Center3D = new Vector3(0, 0, 0);

        public const int SCORE_PER_ORBIT = 1;
        
        public const float RIFT_COLLISION_TOLERANCE = 0.001f;
        public const float ANGLE_TOLERANCE_DEGREES = 0.005f;
        public const float CHARACTER_OFFSET_FROM_ORBIT = 0.25f;
        
        public const string INJECTION_ID_CENTRE = "Centre";
        public const string INJECTION_ID_RIFT_GENERATOR = "RiftGenerator";
        public const string INJECTION_ID_SPIKE_GENERATOR = "SpikeGenerator";
        
        public const string WINDOW_PREFAB_PATH = "Prefabs/UI/Windows/"; 
        
        public const string SCENE_GAMEPLAY = "Gameplay";
        
        public const string PLAYER_DATA_SCORE_SECTION = "PLAYER_DATA_SCORE_SECTION";
        
        public const float OBSTACLE_Y_OFFSET = -0.09f;
    }
}
