using UnityEngine;

namespace SnoozyRat
{
    public class SR_DemoCharacterEquipment : MonoBehaviour
    {
        [SerializeField] private GameObject[] equipmentItems;
        [SerializeField] private bool equipmentStartVisibility;

        [Space]
        [SerializeField] private bool randomizeOnStart = false;
        [SerializeField] private bool randomizeOnSpacebar = true;

        private GameObject currentEquipment;
        private bool showEquipment;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            currentEquipment = equipmentItems[0];
            SetEquipment(equipmentStartVisibility);
            if (randomizeOnStart)
                RandomizeEquipment();
        }

        // Update is called once per frame
        void Update()
        {
            if (randomizeOnSpacebar && Input.GetKeyUp(KeyCode.Space))
            {
                RandomizeEquipment();
            }
        }

        public void RandomizeEquipment()
        {
            currentEquipment = equipmentItems[Random.Range(0, equipmentItems.Length)];
            UpdateVisibility();
        } 

        public void SetEquipment(bool show)
        {
            showEquipment = show;
            UpdateVisibility();
        }

        public void ShowEquipment()
        {
            SetEquipment(true);
        }

        public void HideEquipment()
        {
            SetEquipment(false);
        }

        private void UpdateVisibility()
        {
            foreach (GameObject item in equipmentItems)
                item.SetActive(false);

            currentEquipment.SetActive(showEquipment);
        }
    }
}
