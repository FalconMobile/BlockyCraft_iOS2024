using Loadings;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelPlay;

public class GameMain : MonoBehaviour
{
    public static GameMain Instance;

    public VoxelPlayEnvironment Env;

    [Header("Loading references")]
    [SerializeField] private MobileTouchInput.MobileInput _mobileInput;
    [SerializeField] private CustomNetworkManager _networkManager;

    private LoadingInTurn MainLoading;

    private IEnumerator Start()
    {
        Instance = this;
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        yield return Pool.WaitForEndOfFrame;
        CreateLoadingTurn();
        MainLoading.Load();
    }

    private void SceneManager_sceneLoaded(Scene loadedScene, LoadSceneMode arg1)
    {
        Debug.LogError($"SceneManager_sceneLoaded {loadedScene.name}");
    }

    private void CreateLoadingTurn()
    { 
        MainLoading = new LoadingInTurn();

        MainLoading.AddStep(() => _networkManager.Create());
        MainLoading.AddStep(() => _networkManager.HostGame());

        //MainLoading.AddStep(() => _mobileInput.SetForEnvironment(Env));

        //MainLoading.AddStep(() => Env.gameObject.SetActive(true));//TO DO set it active by default but with delayed initialization with Iloading interface

    }
}