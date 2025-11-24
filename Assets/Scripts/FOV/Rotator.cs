using UnityEngine;

namespace FOV.Demo
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private Transform target = null;

        private void Update() => transform.RotateAround(target ? target.position : Vector3.zero, Vector3.up, speed * Time.deltaTime);
    }
}