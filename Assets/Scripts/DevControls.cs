using UnityEngine;

public class DevControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FungalNetworkManager networkManager;

    [Header("Test Values")]
    [SerializeField] private float nodeDamageAmount = 10f;
    [SerializeField] private float nodeHealAmount = 10f;
    [SerializeField] private float playerDamagePercent = 10f;

    [Header("UI")]
    [SerializeField] private bool showOverlay = true;

    private void Awake()
    {
        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<FungalNetworkManager>();
        }
    }

    private void Update()
    {
        if (networkManager == null) return;

        if (Input.GetKeyDown(KeyCode.F1))
            showOverlay = !showOverlay;

        if (Input.GetKeyDown(KeyCode.F2))
            networkManager.DamageAllNodes(nodeDamageAmount);

        if (Input.GetKeyDown(KeyCode.F3))
            networkManager.HealAllNodes(nodeHealAmount);

        if (Input.GetKeyDown(KeyCode.F4))
            networkManager.ApplyDirectPlayerDamagePercent(playerDamagePercent);

        if (Input.GetKeyDown(KeyCode.F5))
            networkManager.RestoreAllNodesFully();

        if (Input.GetKeyDown(KeyCode.F6))
            Debug.Log($"Network Health: {networkManager.NetworkHealthPercent:0.0}%");
    }

    private void OnGUI()
    {
        if (!showOverlay || networkManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 200), GUI.skin.box);
        GUILayout.Label("DEV CONTROLS");
        GUILayout.Label($"Network Health: {networkManager.NetworkHealthPercent:0.0}%");
        GUILayout.Label("F1 - Toggle this overlay");
        GUILayout.Label($"F2 - Damage all nodes ({nodeDamageAmount})");
        GUILayout.Label($"F3 - Heal all nodes ({nodeHealAmount})");
        GUILayout.Label($"F4 - Direct player damage ({playerDamagePercent}% network)");
        GUILayout.Label("F5 - Fully restore all nodes");
        GUILayout.Label("F6 - Print network health to console");
        GUILayout.EndArea();
    }
}