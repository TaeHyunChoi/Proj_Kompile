using Script.Asset.Provider;
using Script.Global.Input.Provider;
using Script.Global.Manager;
using Script.Map.Provider;
using System;
using UnityEngine;

public class EditFieldMain : MonoBehaviour
{
    private FieldManager _fieldManager;
    private IngameInputProvider _inputProvider;
    private MapRepoProvider _mapRepoProvider;

    private void Awake()
    {
        AssetRepoProvider.Initialize();
        
        _inputProvider = new IngameInputProvider();
        _mapRepoProvider = new MapRepoProvider();
        _fieldManager = transform.GetComponent<FieldManager>();
    }

    private async void Start()
    {
        var init = _fieldManager.Initialize(_inputProvider, _mapRepoProvider);
        await init;
    }
}
