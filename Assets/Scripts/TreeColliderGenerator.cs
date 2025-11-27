using UnityEngine;

public class TreeColliderGenerator : MonoBehaviour
{
	[Tooltip("The Terrain object that holds the trees.")]
	public Terrain targetTerrain;
    public bool childrenCollider = true;

	public void Start()
	{
		GenerateColliders();
	}
	
	public void GenerateColliders()
	{
		if (targetTerrain == null)
		{
			Debug.LogError("Target Terrain is not assigned!");
			return;
		}

		TerrainData terrainData = targetTerrain.terrainData;
		TreeInstance[] treeInstances = terrainData.treeInstances;
		TreePrototype[] prototypes = terrainData.treePrototypes;
		int colliderCount = 0;

		Transform terrainTransform = targetTerrain.transform;

		// Loop through all painted trees
		foreach (TreeInstance instance in treeInstances)
		{
			// Ensure the index is valid
			if (instance.prototypeIndex >= prototypes.Length) continue;

			// Get the original prefab from the prototype list
			GameObject originalPrefab = prototypes[instance.prototypeIndex].prefab;
			
			//Create the host GameObject
			GameObject colliderHost = new GameObject($"TreeCollider_{colliderCount}");
			colliderHost.transform.position = Vector3.Scale(instance.position, terrainData.size) + terrainTransform.position;
			colliderHost.transform.localScale = Vector3.one * instance.widthScale;
			colliderHost.transform.parent = this.transform;

			// Try to copy a collider from the original prefab
			CopyColliderFromPrefab(originalPrefab, colliderHost);
			colliderCount++;
		}
	}

	private void CopyColliderFromPrefab(GameObject prefab, GameObject host)
	{
		Collider originalCollider = childrenCollider ? prefab.GetComponentInChildren<Collider>() : prefab.GetComponent<Collider>();
		if (originalCollider == null) return;

		switch (originalCollider)
		{
			case BoxCollider originalBoxCollider:
				BoxCollider newBoxCollider = host.AddComponent<BoxCollider>();
				newBoxCollider.size = originalBoxCollider.size;
				newBoxCollider.center = originalBoxCollider.center;
				return;

			case SphereCollider originalSphereCollider:
				SphereCollider newSphereCollider = host.AddComponent<SphereCollider>();
				newSphereCollider.radius = originalSphereCollider.radius;
				newSphereCollider.center = originalSphereCollider.center;
				return;

			case CapsuleCollider originalCapsuleCollider:
				CapsuleCollider newCapsuleCollider = host.AddComponent<CapsuleCollider>();
				newCapsuleCollider.radius = originalCapsuleCollider.radius;
				newCapsuleCollider.height = originalCapsuleCollider.height;
				newCapsuleCollider.center = originalCapsuleCollider.center;
				return;

			case MeshCollider originalMeshCollider:
				MeshCollider newMeshCollider = host.AddComponent<MeshCollider>();
				newMeshCollider.sharedMesh = originalMeshCollider.sharedMesh;
				newMeshCollider.convex = true;
				return;
		}
	}
}
