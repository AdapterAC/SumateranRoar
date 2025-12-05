using Unity.Netcode;
using UnityEngine;

public class IndicatorBarController : NetworkBehaviour
{
    
    [SerializeField] private GameObject indicatorBar;
    private void Start()
    {
        indicatorBar.SetActive(false);
        if (!IsOwner) return;
        indicatorBar.SetActive(true);
    }
}
