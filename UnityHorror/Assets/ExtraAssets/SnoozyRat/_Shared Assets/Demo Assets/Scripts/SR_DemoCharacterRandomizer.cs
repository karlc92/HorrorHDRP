using UnityEngine;

namespace SnoozyRat
{
    public class SR_DemoCharacterRandomizer : MonoBehaviour
    {
        [SerializeField] private RandomizationProperty[] propertiesToRandomize;
        [SerializeField] private bool randomizeOnStart = false;
        [SerializeField] private bool randomizeOnSpacebar = true;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (randomizeOnStart)
                RandomizeAllProperties();
        }

        // Update is called once per frame
        void Update()
        {
            if (randomizeOnSpacebar && Input.GetKeyUp(KeyCode.Space))
            {
                RandomizeAllProperties();
            }
        }

        public void RandomizeAllProperties()
        {
            if (propertiesToRandomize.Length < 1)
            {
                Debug.LogError("No properties set for randomization!");
                return;
            }

            for (int i = 0; i < propertiesToRandomize.Length; i++)
            {
                RandomizeProperty(propertiesToRandomize[i]);
            }
        }

        public void RandomizeProperty(RandomizationProperty property)
        {
            if (property.meshGroups.Length < 1)
            {
                Debug.LogError("No meshes set for randomization property!");
                return;
            }

            // Disable all Mesh Groups
            for (int i = 0; i < property.meshGroups.Length; i++)
            {
                RandomizationMeshGroup meshGroup = property.meshGroups[i];

                for (int j = 0; j < meshGroup.meshes.Length; j++)
                {
                    Renderer mesh = meshGroup.meshes[j];

                    mesh.gameObject.SetActive(false);
                }
            }

            // Enable randomly selected Mesh Group
            int randomMeshGroup = Random.Range(0, property.meshGroups.Length);
            RandomizationMeshGroup activeMeshGroup = property.meshGroups[randomMeshGroup];
            for (int j = 0; j < activeMeshGroup.meshes.Length; j++)
            {
                Renderer mesh = activeMeshGroup.meshes[j];

                mesh.gameObject.SetActive(true);
            }
        }
    }

    [System.Serializable]
    public class RandomizationProperty
    {
        [Tooltip("Name is just for visual organization in the editor")]
        public string name;
        [Tooltip("Meshes to be affected by this randomization property")]
        public RandomizationMeshGroup[] meshGroups;
    }

    [System.Serializable]
    public class RandomizationMeshGroup 
    {
        public Renderer[] meshes;
    }
}
