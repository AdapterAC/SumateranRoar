using UnityEngine;

namespace ithappy.Animals_FREE
{
    public class ThirdPersonCamera : PlayerCamera
    {
        [SerializeField, Range(0f, 2f)]
        private float m_Offset = 1.5f;
        [SerializeField, Range(0f, 360f)]
        private float m_CameraSpeed = 90f;
        [SerializeField, Range(1f, 20f)]
        private float m_Smooth = 10f; // Smoothing factor seperti di ThirdPersonOrbitCamBasic

        private Vector3 m_LookPoint;
        private Vector3 m_TargetPos;
        private Vector3 m_SmoothTargetPos; // Smooth interpolated position
        private Vector3 m_SmoothLookPoint; // Smooth interpolated look point

        private void Start()
        {
            if (m_Player != null)
            {
                // Initialize smooth positions
                UpdateTargetPosition();
                m_SmoothTargetPos = m_TargetPos;
                m_SmoothLookPoint = m_LookPoint;
                m_Transform.position = m_TargetPos;
            }
        }

        private void LateUpdate()
        {
            if (m_Player == null) return;
            
            UpdateTargetPosition();
            Move(Time.deltaTime);
        }

        public override void SetInput(in Vector2 delta, float scroll)
        {
            base.SetInput(delta, scroll);
        }

        private void UpdateTargetPosition()
        {
            var dir = new Vector3(0, 0, -m_Distance);
            var rot = Quaternion.Euler(m_Angles.x, m_Angles.y, 0f);

            m_LookPoint = m_Player.position + m_Offset * Vector3.up;
            m_TargetPos = m_LookPoint + rot * dir;
        }

        private void Move(float deltaTime)
        {
            // Smooth interpolation seperti di ThirdPersonOrbitCamBasic
            m_SmoothTargetPos = Vector3.Lerp(m_SmoothTargetPos, m_TargetPos, m_Smooth * deltaTime);
            m_SmoothLookPoint = Vector3.Lerp(m_SmoothLookPoint, m_LookPoint, m_Smooth * deltaTime);

            // Set position dan rotation dengan smooth values
            m_Transform.position = m_SmoothTargetPos;
            m_Transform.LookAt(m_SmoothLookPoint);

            // Update target if exists
            if (m_Target != null)
            {
                m_Target.position = m_Transform.position + m_Transform.forward * TargetDistance;
            }
        }
    }
}