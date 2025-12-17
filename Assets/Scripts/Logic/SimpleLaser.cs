using UnityEngine;

namespace Logic
{
    [RequireComponent(typeof(LineRenderer))]
    public class SimpleLaser : MonoBehaviour
    {
        public float maxDistance = 5.0f; // 레이저 최대 길이 (5m)
        private LineRenderer lr;

        void Start()
        {
            lr = GetComponent<LineRenderer>();
            lr.positionCount = 2; // 시작점, 끝점
        }

        void Update()
        {
            // 1. 시작점: 현재 손의 위치
            lr.SetPosition(0, transform.position);

            // 2. 끝점 계산 (레이캐스트)
            RaycastHit hit;
            // UI는 OVRRaycaster가 처리하지만, 시각적으로는 물리적인 벽이나 바닥에 닿는 걸 보여줌
            if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance))
            {
                // 무언가에 닿았으면 거기까지만 그림
                lr.SetPosition(1, hit.point);
            }
            else
            {
                // 허공이면 최대 길이만큼 그림
                lr.SetPosition(1, transform.position + (transform.forward * maxDistance));
            }
        }
    }
}