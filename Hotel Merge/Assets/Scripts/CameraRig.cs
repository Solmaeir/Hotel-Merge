using UnityEngine;

namespace HotelMerge
{
    /// <summary>Frames the hotel so its width always fits the (portrait) screen; scrolls between floors.</summary>
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { Title, Hotel }

        public Mode mode = Mode.Title;
        public Hotel3D hotel;
        Camera cam;
        float focusY = 7f, targetY = 7f, velocity, yaw = -14f;

        Camera Cam => cam ? cam : cam = GetComponent<Camera>();

        float MinY => 4.6f;
        float MaxY => Mathf.Max(MinY, hotel.TopY - 3.5f);

        public void Scroll(float worldDelta) => targetY = Mathf.Clamp(targetY + worldDelta, MinY, MaxY);
        public void StepFloor(int dir) => Scroll(dir * Hotel3D.FloorH * 1.5f);
        public bool CanGoUp => targetY < MaxY - 0.05f;
        public bool CanGoDown => targetY > MinY + 0.05f;

        public void FocusOnFloor(int floor)
        {
            targetY = Mathf.Clamp(Hotel3D.FloorBase(floor) + Hotel3D.FloorH * 0.5f + 1f, MinY, MaxY);
        }

        /// <summary>World units per screen pixel at the building's depth (for drag scrolling).</summary>
        public float UnitsPerPixel => 2f * Distance() * Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;

        float Distance()
        {
            float aspect = Mathf.Min(Cam.aspect, 0.62f);
            float halfWidth = Hotel3D.OuterHalfW + (mode == Mode.Title ? 1.8f : 0.8f);
            return halfWidth / (Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect);
        }

        /// <summary>Jumps to the resting pose immediately (used when building the scene in the editor).</summary>
        public void Snap()
        {
            targetY = MinY;
            focusY = targetY;
            Pose(11f);
        }

        void Pose(float pitch)
        {
            var rot = Quaternion.Euler(pitch, yaw, 0);
            var target = new Vector3(0, focusY, Hotel3D.Depth * 0.5f);
            transform.SetPositionAndRotation(target - rot * Vector3.forward * Distance(), rot);
        }

        void LateUpdate()
        {
            if (!hotel) return;
            targetY = Mathf.Clamp(targetY, MinY, MaxY);
            float pitch = 11f;
            float wantedYaw = -14f;
            if (mode == Mode.Title)
            {
                wantedYaw = -14f + Mathf.Sin(Time.time * 0.25f) * 22f;
                targetY = Mathf.Lerp(MinY, MaxY, 0.5f) + 1.5f;
                pitch = 9f;
            }
            yaw = Mathf.Lerp(yaw, wantedYaw, 1f - Mathf.Exp(-3f * Time.deltaTime));
            focusY = Mathf.SmoothDamp(focusY, targetY, ref velocity, 0.25f);
            Pose(pitch);
        }
    }
}
