using UnityEngine;

namespace SnoozyRat
{
    public class SR_DemoCharacterBlendshapeRandomizer : MonoBehaviour
    {
        [Tooltip("Meshes containing any of the blenshapes included in the Properties To Randomize")]
        public SkinnedMeshRenderer[] meshes;
        [SerializeField] private BlendshapeRandomizationProperty[] propertiesToRandomize;
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
                Debug.LogError("No blendshape properties set for randomization!");
                return;
            }

            if (meshes.Length < 1)
            {
                Debug.LogError("No meshes set for randomization!");
                return;
            }

            for (int i = 0; i < propertiesToRandomize.Length; i++)
            {
                RandomizeProperty(propertiesToRandomize[i]);
            }
        }

        public void RandomizeProperty(BlendshapeRandomizationProperty property)
        {
            if (meshes.Length < 1)
            {
                Debug.LogError("No meshes set for randomization!");
                return;
            }

            if (property.minValue > property.maxValue)
            {
                Debug.LogError("Min Value is greater than Max Value for Blendshape: " + property.blenshapeName);
                return;
            }

            for (int i = 0; i < meshes.Length; i++)
            {
                SkinnedMeshRenderer mesh = meshes[i];
                int blendshapeIndex = mesh.sharedMesh.GetBlendShapeIndex(property.blenshapeName);
                if (blendshapeIndex >= 0)
                {
                    mesh.SetBlendShapeWeight(blendshapeIndex, Random.Range(property.minValue, property.maxValue));
                }
            }
        }
    }

    [System.Serializable]
    public class BlendshapeRandomizationProperty
    {
        [Tooltip("Name used to idendify the blendshape on the mesh")]
        public string blenshapeName;
        [Tooltip("Minimum value the blendshape weight might be set to (0 - 100)")]
        [Range(0, 100)]
        public int minValue = 0;
        [Tooltip("Maximum value the blendshape weight might be set to (0 - 100)")]
        [Range(0, 100)]
        public int maxValue = 100;
    }
}
