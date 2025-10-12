using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AuthManager : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
    }
}
