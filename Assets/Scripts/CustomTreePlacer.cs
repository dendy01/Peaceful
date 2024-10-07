using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CustomTreePlacer : MonoBehaviour
{
    public Terrain terrain;
    public List<GameObject> treePrefabs;
    [Range(0, 5000)]
    public int numberOfTrees = 500;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    [Range(0f, 1f)]
    public float minHeightPercent = 0f;
    [Range(0f, 1f)]
    public float maxHeightPercent = 0.7f;
    [Range(0f, 90f)]
    public float maxSlopeAngle = 30f;

    [Header("Editor Tools")]
    public float brushRadius = 5f;
    public int treesPerBrush = 5;

    [ContextMenu("Place Trees")]
    public void PlaceTrees()
    {
        if (terrain == null || treePrefabs.Count == 0)
        {
            Debug.LogError("Terrain or tree prefabs are not set!");
            return;
        }

        ClearAllTrees();

        int placedTrees = 0;
        int maxAttempts = numberOfTrees * 10;
        for (int i = 0; i < maxAttempts && placedTrees < numberOfTrees; i++)
        {
            if (TryPlaceTree(GetRandomPositionOnTerrain()))
            {
                placedTrees++;
            }
        }

        Debug.Log($"Placed {placedTrees} trees out of {numberOfTrees} requested.");
    }

    public void ClearAllTrees()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    private Vector3 GetRandomPositionOnTerrain()
    {
        return new Vector3(
            Random.Range(0f, terrain.terrainData.size.x),
            0f,
            Random.Range(0f, terrain.terrainData.size.z)
        );
    }

    public bool TryPlaceTree(Vector3 position)
    {
        position.y = terrain.SampleHeight(position);
        Debug.Log($"Trying to place tree at {position}"); // Добавляем отладочное сообщение

        float normalizedHeight = position.y / terrain.terrainData.size.y;
        if (normalizedHeight < minHeightPercent || normalizedHeight > maxHeightPercent)
        {
            Debug.Log($"Tree not placed: Height out of range. Normalized height: {normalizedHeight}"); // Отладочное сообщение
            return false;
        }

        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(position.x / terrain.terrainData.size.x, position.z / terrain.terrainData.size.z);
        float slope = Vector3.Angle(normal, Vector3.up);
        if (slope > maxSlopeAngle)
        {
            Debug.Log($"Tree not placed: Slope too steep. Slope angle: {slope}"); // Отладочное сообщение
            return false;
        }

        GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
        GameObject tree = Instantiate(treePrefab, position, Quaternion.identity, transform);
        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        float randomScale = Random.Range(minScale, maxScale);
        tree.transform.localScale = Vector3.one * randomScale;

        Debug.Log($"Tree placed successfully at {position}"); // Отладочное сообщение
        return true;
    }


    private void OnDrawGizmosSelected()
    {
        if (terrain == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector3 size = terrain.terrainData.size;
        Vector3 minPos = terrain.transform.position + new Vector3(0, size.y * minHeightPercent, 0);
        Vector3 maxPos = terrain.transform.position + new Vector3(size.x, size.y * maxHeightPercent, size.z);
        Gizmos.DrawCube((minPos + maxPos) * 0.5f, maxPos - minPos);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CustomTreePlacer))]
public class CustomTreePlacerEditor : Editor
{
    private CustomTreePlacer treePlacer;
    private bool isPlacingTrees = false;
    private bool isRemovingTrees = false;

    private void OnEnable()
    {
        treePlacer = (CustomTreePlacer)target;
    }

    public override void OnInspectorGUI()
    {
        Debug.Log("OnInspectorGUI is called"); // Добавьте эту строку в начало метода

        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Place All Trees"))
        {
            treePlacer.PlaceTrees();
        }

        if (GUILayout.Button("Clear All Trees"))
        {
            treePlacer.ClearAllTrees();
        }

        EditorGUILayout.Space();

        isPlacingTrees = GUILayout.Toggle(isPlacingTrees, "Place Trees Tool", "Button");
        isRemovingTrees = GUILayout.Toggle(isRemovingTrees, "Remove Trees Tool", "Button");

        if (isPlacingTrees && isRemovingTrees)
        {
            isPlacingTrees = false;
            isRemovingTrees = false;
        }

        if (isPlacingTrees || isRemovingTrees)
        {
            EditorGUILayout.HelpBox("Left click to place/remove trees. Hold Shift for continuous placement/removal.", MessageType.Info);
        }

        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        Debug.Log("OnSceneGUI is called"); // Добавьте эту строку в начало метода

        if (!isPlacingTrees && !isRemovingTrees) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
        {
            Handles.color = isPlacingTrees ? Color.green : Color.red;
            Handles.DrawWireDisc(hit.point, hit.normal, treePlacer.brushRadius);

            // Отображаем отладочную информацию
            Handles.Label(hit.point + Vector3.up * 2, $"Hit point: {hit.point}");

            if (e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && e.shift))
            {
                if (e.button == 0)
                {
                    if (isPlacingTrees)
                    {
                        PlaceTreesInBrush(hit.point);
                        Debug.Log($"Attempting to place trees at {hit.point}"); // Добавляем отладочное сообщение
                    }
                    else if (isRemovingTrees)
                    {
                        RemoveTreesInBrush(hit.point);
                    }
                    e.Use();
                }
            }
        }

        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        SceneView.RepaintAll();
    }

    private void PlaceTreesInBrush(Vector3 center)
    {
        for (int i = 0; i < treePlacer.treesPerBrush; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * treePlacer.brushRadius;
            Vector3 position = center + new Vector3(randomOffset.x, 0, randomOffset.y);
            bool placed = treePlacer.TryPlaceTree(position);
            Debug.Log($"Attempted to place tree at {position}. Success: {placed}"); // Добавляем отладочное сообщение
        }
    }

    private void RemoveTreesInBrush(Vector3 center)
    {
        List<GameObject> treesToRemove = new List<GameObject>();
        foreach (Transform child in treePlacer.transform)
        {
            if (Vector3.Distance(child.position, center) <= treePlacer.brushRadius)
            {
                treesToRemove.Add(child.gameObject);
            }
        }

        foreach (GameObject tree in treesToRemove)
        {
            DestroyImmediate(tree);
        }
    }
}
#endif
