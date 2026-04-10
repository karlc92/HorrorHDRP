using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_Atmo
{

public class DisableAtStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }

}
}